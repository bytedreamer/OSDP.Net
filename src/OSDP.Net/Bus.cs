using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using OSDP.Net.Connections;
using OSDP.Net.Messages;
using OSDP.Net.Messages.ACU;
using OSDP.Net.Model.ReplyData;
using OSDP.Net.Tracing;
#if NETSTANDARD2_0
using OSDP.Net.Utilities;
#endif
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace OSDP.Net
{
    /// <summary>
    /// A group of OSDP devices sharing communications
    /// </summary>
    internal class Bus : IDisposable
    {
        private const byte DriverByte = 0xFF;

        public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(200);
        
        private readonly SortedSet<Device> _configuredDevices = new ();
        private readonly object _configuredDevicesLock = new ();
        private readonly Dictionary<byte, bool> _lastOnlineConnectionStatus = new ();
        private readonly Dictionary<byte, bool> _lastSecureConnectionStatus = new ();

        private readonly ILogger<ControlPanel> _logger;
        private readonly TimeSpan _pollInterval;
        private readonly BlockingCollection<Reply> _replies;
        private readonly Action<TraceEntry> _tracer;
        private readonly AutoResetEvent _commandAvailableEvent = new (false);
        private readonly AutoResetEvent _shutdownComplete = new (false);
        
        private CancellationTokenSource _cancellationTokenSource;

        public Bus(IOsdpConnection connection, BlockingCollection<Reply> replies, TimeSpan pollInterval,
            Action<TraceEntry> tracer,
            // ReSharper disable once ContextualLoggerProblem
            ILogger<ControlPanel> logger = null)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _replies = replies ?? throw new ArgumentNullException(nameof(replies));
            _pollInterval = pollInterval;
            _tracer = tracer;
            Id = Guid.NewGuid();
            _logger = logger;
        }

        private bool IsPolling => _pollInterval > TimeSpan.Zero;

        /// <summary>
        /// Unique identifier of the bus
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// The connection used by the bus. Each connection may only belong to one bus.
        /// </summary>
        public IOsdpConnection Connection { get; private set; }

        public IEnumerable<byte> ConfigureDeviceAddresses => _configuredDevices.Select(device => device.Address);

        public void Dispose()
        {
            _shutdownComplete?.Dispose();
            _commandAvailableEvent?.Dispose();
            _cancellationTokenSource?.Dispose();
        }

        private TimeSpan IdleLineDelay(int numberOfBytes)
        {
            return TimeSpan.FromSeconds((1.0 / Connection.BaudRate) * (10.0 * numberOfBytes));
        }

        /// <summary>
        /// Closes down the connection
        /// </summary>
        public async Task Close()
        {            
            var cancellationTokenSource = _cancellationTokenSource;
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                _shutdownComplete.WaitOne(TimeSpan.FromSeconds(1));
                _cancellationTokenSource = null;
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Send a command to a device
        /// </summary>
        /// <param name="command">Details about the command</param>
        public void SendCommand(Command command)
        {
            var foundDevice = _configuredDevices.First(device => device.Address == command.Address);            
            foundDevice.SendCommand(command);
            _commandAvailableEvent.Set();
        }

        /// <summary>
        /// Add a device to the bus
        /// </summary>
        /// <param name="address">Address of the device</param>
        /// <param name="useCrc">Use CRC for error checking</param>
        /// <param name="useSecureChannel">Use a secure channel to communicate</param>
        /// <param name="secureChannelKey">Set the secure channel key, default is used if not specified</param>
        public void AddDevice(byte address, bool useCrc, bool useSecureChannel, byte[] secureChannelKey = null)
        {
            lock (_configuredDevicesLock)
            {
                var foundDevice = _configuredDevices.FirstOrDefault(device => device.Address == address);
                
                if (foundDevice != null)
                {
                    _configuredDevices.Remove(foundDevice);
                }

                var addedDevice = new Device(address, useCrc, useSecureChannel, secureChannelKey);

                _configuredDevices.Add(addedDevice);
            }
        }

        /// <summary>
        /// Remove a device from the bus
        /// </summary>
        /// <param name="address">Address of the device</param>
        public void RemoveDevice(byte address)
        {
            lock (_configuredDevicesLock)
            {
                var foundDevice = _configuredDevices.FirstOrDefault(device => device.Address == address);
                if (foundDevice == null) return;
                
                _configuredDevices.Remove(foundDevice);
            }
        }

        /// <summary>
        /// Is the device currently online
        /// </summary>
        /// <param name="address">Address of the device</param>
        /// <returns>True if the device is online</returns>
        public bool IsOnline(byte address)
        {
            var foundDevice = _configuredDevices.First(device => device.Address == address);
            
            return  foundDevice.IsConnected;
        }

        /// <summary>
        /// Start polling the devices on the bus
        /// </summary>
        /// <returns></returns>
        public void StartPolling()
        {
            var cancellationTokenSource = _cancellationTokenSource;
            if (cancellationTokenSource != null) return;
            _cancellationTokenSource = cancellationTokenSource = new CancellationTokenSource();
            
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    await PollingLoop(cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch(Exception exception)
                {
                    _logger?.LogError(exception, $"[{Connection}] Unexpected exception in polling loop. Connection ID:{Id}.");
                }
                finally
                {
                    _shutdownComplete.Set();
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Poll the the devices on the bus
        /// </summary>
        /// <returns></returns>
        private async Task PollingLoop(CancellationToken cancellationToken)
        {
            DateTime lastMessageSentTime = DateTime.MinValue;
            using var delayTime = new AutoResetEvent(false);
            
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!Connection.IsOpen)
                {
                    try
                    {
                        Connection.Open();
                    }
                    catch (Exception exception)
                    {
                        _logger?.LogError(exception, $"[{Connection}] Error while opening connection");
                        foreach (var device in _configuredDevices.ToArray())
                        {
                            ResetDevice(device);
                            UpdateConnectionStatus(device);
                        }
                        
                        delayTime.WaitOne(TimeSpan.FromSeconds(5));

                        continue;
                    }
                }

                if (IsPolling)
                {
                    // Allow for immediate processing of commands in queue or incoming multipart messages
                    while (_pollInterval - (DateTime.UtcNow - lastMessageSentTime) > TimeSpan.Zero &&
                           !_configuredDevices.Any(device1 => device1.HasQueuedCommand) && 
                           !_configuredDevices.Any(device2 => device2.IsReceivingMultipartMessage))
                    {
                        delayTime.WaitOne(TimeSpan.FromMilliseconds(10));
                    }

                    lastMessageSentTime = DateTime.UtcNow;

                    if (!_configuredDevices.Any())
                    {
                        continue;
                    }
                }
                else
                {
                    // Keep CPU usage down while waiting for next command
                    _commandAvailableEvent.WaitOne(TimeSpan.FromMilliseconds(10));
                }

                foreach (var device in _configuredDevices.ToArray())
                {
                    // Right now it always sends sequence 0
                    if (!IsPolling)
                    {
                        device.MessageControl.ResetSequence();
                    }

                    // Requested delay for multi-messages and resets
                    if (device.RequestDelay > DateTime.UtcNow)
                    {
                        continue;
                    }
                    
                    var command = device.GetNextCommandData(IsPolling);
                    if (command == null || WaitingForNextMultiMessage(command, device.IsSendingMultipartMessage))
                    {
                        continue;
                    }
                    
                    Reply reply;
                    
                    try
                    {
                        // Reset the device if it loses connection
                        if (IsPolling && UpdateConnectionStatus(device) && !device.IsConnected)
                        {
                            ResetDevice(device);
                        }
                        else if(IsPolling && !device.IsConnected)
                        {
                            device.RequestDelay = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                        }
                    }
                    catch (Exception exception)
                    {
                        _logger?.LogError(exception, $"[{Connection}] Error while notifying connection status for address {command.Address}");
                    }

                    try
                    {
                        reply = await SendCommandAndReceiveReply(command, device, cancellationToken).ConfigureAwait(false);

                        // Prevent plain text message replies when secure channel has been established
                        // The busy and Nak reply types are a special case which is allowed to be sent as insecure message on a secure channel
                        // Workaround for KeySet command sending back an clear text Ack
                        if (reply.Type != ReplyType.Busy && reply.Type != ReplyType.Nak && device.UseSecureChannel &&
                            device.IsSecurityEstablished && !reply.IsSecureMessage && command.Type != 0x75)
                        {
                            _logger?.LogWarning(
                                "A plain text message was received when the secure channel had been established");
                            device.CreateNewRandomNumber();
                            ResetDevice(device);
                            continue;
                        }
                    }
                    catch (TimeoutException exception)
                    {
                        switch (IsPolling)
                        {
                            // Make sure the security is reset properly if reader goes offline
                            case true when device.IsSecurityEstablished && !IsOnline(command.Address):
                                ResetDevice(device);
                                break;
                            default:
                                _logger?.LogDebug($"[{Connection}] Retrying command {command} on connection {Id} because \"{exception.Message}\".");
                                device.RetryCommand(command);
                                break;
                        }

                        continue;
                    }
                    catch (Exception exception)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            _logger?.LogError(exception,
                                $"[{Connection}] Error while sending command {command} to address {command.Address}. Connection {Id}");
                        }

                        continue;
                    }

                    try
                    {
                        ProcessReply(reply, device);
                    }
                    catch (Exception exception)
                    {
                        _logger?.LogError(exception, $"[{Connection}] Error while processing reply {reply} from address {reply.Address}");
                        continue;
                    }

                    delayTime.WaitOne(IdleLineDelay(2));
                }
            }

            // Polling task is complete. Time to close the connection.
            try
            { 
                Connection.Close();
            }
            catch(Exception exception)
            {
                _logger?.LogError(exception, $"[{Connection}] Error while closing connection {Id}.");
            }
        }

        private static bool WaitingForNextMultiMessage(Command command, bool sendingMultiMessage)
        {
            return sendingMultiMessage && command is not FileTransferCommand;
        }

        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Determine if the connection status needs to be updated
        /// </summary>
        /// <param name="device"></param>
        /// <returns>Return true if the connection status changed</returns>
        private bool UpdateConnectionStatus(Device device)
        {
            bool isConnected = device.IsConnected;
            bool isSecureChannelEstablished = device.IsSecurityEstablished;

            // Default to false when initializing status checks
            _lastOnlineConnectionStatus.TryAdd(device.Address, false);
            _lastSecureConnectionStatus.TryAdd(device.Address, false);
            
            bool onlineConnectionChanged = _lastOnlineConnectionStatus[device.Address] != isConnected;
            bool secureChannelStatusChanged = _lastSecureConnectionStatus[device.Address] != isSecureChannelEstablished;

            if (!onlineConnectionChanged && !secureChannelStatusChanged) return false;

            _lastOnlineConnectionStatus[device.Address] = isConnected;
            _lastSecureConnectionStatus[device.Address] = isSecureChannelEstablished;
            
            var handler = ConnectionStatusChanged;
            handler?.Invoke(this,
                new ConnectionStatusEventArgs(device.Address, isConnected, device.IsSecurityEstablished));

            return true;
        }

        private void ProcessReply(Reply reply, Device device)
        {
            // Request from PD to reset connection
            if (device.IsConnected && reply.Sequence == 0)
            {
                ResetDevice(device);
                return;
            }
            
            if (!reply.IsValidReply)
            {
                return;
            }

            if (reply.IsSecureMessage)
            {
                var mac = device.GenerateMac(reply.MessageForMacGeneration.ToArray(), false);
                if (!reply.IsValidMac(mac))
                {
                    ResetDevice(device);
                    return;
                }
            }

            if (reply.Type != ReplyType.Busy)
            {
                device.ValidReplyHasBeenReceived(reply.Sequence);
            }
            else
            {
                return;
            }

            if (reply.Type == ReplyType.Nak)
            {
                var errorCode = (ErrorCode)reply.ExtractReplyData.First();
                if (device.IsSecurityEstablished &&
                    errorCode is ErrorCode.DoesNotSupportSecurityBlock or ErrorCode.CommunicationSecurityNotMet
                        or ErrorCode.UnableToProcessCommand ||
                    errorCode == ErrorCode.UnexpectedSequenceNumber && reply.Sequence > 0)
                {
                    ResetDevice(device);
                }
            }

            switch (reply.Type)
            {
                case ReplyType.CrypticData:
                    device.InitializeSecureChannel(reply);
                    break;
                case ReplyType.InitialRMac:
                    if (!device.ValidateSecureChannelEstablishment(reply))
                    {
                        _logger?.LogError($"[{Connection}] Cryptogram not accepted by address {reply.Address}");
                    }
                    break;
            }

            _replies.Add(reply);
        }

        public void ResetDevice(int address)
        {
            var foundDevice = _configuredDevices.First(device => device.Address == address);
            
            ResetDevice(foundDevice);
        }

        public void SetSendingMultipartMessage(byte address, bool isSendingMultipartMessage)
        {
            var foundDevice = _configuredDevices.First(device => device.Address == address);

            foundDevice.IsSendingMultipartMessage = isSendingMultipartMessage;
        }
        
        public void SetReceivingMultipartMessage(byte address, bool isReceivingMultipartMessage)
        {
            var foundDevice = _configuredDevices.First(device => device.Address == address);

            foundDevice.IsReceivingMultipartMessage = isReceivingMultipartMessage;
        }

        public void SetSendingMultiMessageNoSecureChannel(byte address, bool isSendingMultiMessageNoSecureChannel)
        {
            var foundDevice = _configuredDevices.First(device => device.Address == address);

            foundDevice.IsSendingMultiMessageNoSecureChannel = isSendingMultiMessageNoSecureChannel;
            foundDevice.MessageControl.IsSendingMultiMessageNoSecureChannel = isSendingMultiMessageNoSecureChannel;
            if (isSendingMultiMessageNoSecureChannel)
            {
                foundDevice.CreateNewRandomNumber();
            }
        }

        public void SetRequestDelay(byte address, DateTime requestDelay)
        {
            var foundDevice = _configuredDevices.First(device => device.Address == address);

            foundDevice.RequestDelay = requestDelay;
        }

        private void ResetDevice(Device device)
        {
            device.RequestDelay = DateTime.UtcNow + TimeSpan.FromSeconds(1);
            AddDevice(device.Address, device.MessageControl.UseCrc, device.UseSecureChannel, device.SecureChannelKey);
        }

        private async Task<Reply> SendCommandAndReceiveReply(Command command, Device device, CancellationToken cancellationToken)
        {
            byte[] commandData;
            try
            {
                commandData = command.BuildCommand(device);
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception, $"[{Connection}] Error while building command {command}");
                throw;
            }

            var buffer = new byte[commandData.Length + 1];

            // Section 5.7 states that transmitting device shall guarantee an idle time between packets. This is
            // accomplished by sending a character with all bits set to 1. The driver byte is required by
            // converters and multiplexers to sense when line is idle.
            buffer[0] = DriverByte;
            Buffer.BlockCopy(commandData, 0, buffer, 1, commandData.Length);
 
            await Connection.WriteAsync(buffer).ConfigureAwait(false);

            _tracer(new TraceEntry(TraceDirection.Output, Id, commandData));
            
            using var delayTime = new AutoResetEvent(false);
            delayTime.WaitOne(IdleLineDelay(buffer.Length));

            return await ReceiveReply(command, device, cancellationToken);
        }

        private async Task<Reply> ReceiveReply(Command command, Device device, CancellationToken cancellationToken)
        {
            var replyBuffer = new Collection<byte>();

            if (!await WaitForStartOfMessage(replyBuffer, device.IsSendingMultipartMessage, cancellationToken).ConfigureAwait(false))
            {
                throw new TimeoutException("Timeout waiting for reply message");
            }

            if (!await WaitForMessageLength(replyBuffer, cancellationToken).ConfigureAwait(false))
            {
                throw new TimeoutException("Timeout waiting for reply message length");
            }

            if (!await WaitForRestOfMessage(replyBuffer, ExtractMessageLength(replyBuffer), cancellationToken).ConfigureAwait(false))
            {
                throw new TimeoutException("Timeout waiting for rest of reply message");
            }
            
            _tracer(new TraceEntry(TraceDirection.Input, Id, replyBuffer.ToArray()));

            return Reply.Parse(replyBuffer.ToArray(), Id, command, device);
        }

        private static ushort ExtractMessageLength(IReadOnlyList<byte> replyBuffer)
        {
            return Message.ConvertBytesToUnsignedShort(new[] {replyBuffer[2], replyBuffer[3]});
        }

        private async Task<bool> WaitForRestOfMessage(ICollection<byte> replyBuffer, ushort replyLength, CancellationToken cancellationToken)
        {
            while (replyBuffer.Count < replyLength)
            {
                int maxReadBufferLength = Connection.BaudRate / 40;
                int remainingLength = replyLength - replyBuffer.Count;
                byte[] readBuffer = new byte[Math.Min(maxReadBufferLength, remainingLength)];
                int bytesRead =
                    await TimeOutReadAsync(readBuffer, Connection.ReplyTimeout + IdleLineDelay(readBuffer.Length),
                            cancellationToken)
                        .ConfigureAwait(false);
                if (bytesRead > 0)
                {
                    for (int index = 0; index < bytesRead; index++)
                    {
                        replyBuffer.Add(readBuffer[index]);
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> WaitForMessageLength(ICollection<byte> replyBuffer, CancellationToken cancellationToken)
        {
            while (replyBuffer.Count < 4)
            {
                byte[] readBuffer = new byte[4];
                int bytesRead =
                    await TimeOutReadAsync(readBuffer, Connection.ReplyTimeout, cancellationToken)
                        .ConfigureAwait(false);
                if (bytesRead > 0)
                {
                    for (int index = 0; index < bytesRead; index++)
                    {
                        replyBuffer.Add(readBuffer[index]);
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> WaitForStartOfMessage(ICollection<byte> replyBuffer, bool waitLonger, CancellationToken cancellationToken)
        {
            while (true)
            {
                byte[] readBuffer = new byte[1];
                int bytesRead = await TimeOutReadAsync(readBuffer,
                        Connection.ReplyTimeout + (waitLonger ? TimeSpan.FromSeconds(1) : TimeSpan.Zero),
                        cancellationToken)
                    .ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    return false;
                }

                if (readBuffer[0] != Message.StartOfMessage)
                {
                    continue;
                }

                replyBuffer.Add(readBuffer[0]);
                break;
            }

            return true;
        }

        private async Task<int> TimeOutReadAsync(byte[] buffer, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var timedCancellationTokenSource = new CancellationTokenSource(timeout);
            using var linkedCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timedCancellationTokenSource.Token);
            try
            {
                return await Connection.ReadAsync(buffer, linkedCancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch
            {
                return 0;
            }
        }

        public class ConnectionStatusEventArgs
        {
            public ConnectionStatusEventArgs(byte address, bool isConnected, bool isSecureSessionEstablished)
            {
                Address = address;
                IsConnected = isConnected;
                IsSecureSessionEstablished = isSecureSessionEstablished;
            }

            public byte Address { get; }

            public bool IsConnected { get; }

            public bool IsSecureSessionEstablished { get; }
        }
    }
}
