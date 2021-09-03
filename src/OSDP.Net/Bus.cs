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
using OSDP.Net.Model.ReplyData;
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace OSDP.Net
{
    /// <summary>
    /// A group of OSDP devices sharing communications
    /// </summary>
    internal class Bus
    {
        private const byte DriverByte = 0xFF;

        public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(200);
        private readonly SortedSet<Device> _configuredDevices = new ();
        private readonly object _configuredDevicesLock = new ();
        private readonly IOsdpConnection _connection;
        private readonly Dictionary<byte, bool> _lastConnectionStatus = new ();

        private readonly ILogger<ControlPanel> _logger;
        private readonly TimeSpan _pollInterval;
        private readonly BlockingCollection<Reply> _replies;

        private bool _isShuttingDown;

        public Bus(IOsdpConnection connection, BlockingCollection<Reply> replies, TimeSpan pollInterval,
            // ReSharper disable once ContextualLoggerProblem
            ILogger<ControlPanel> logger = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _replies = replies ?? throw new ArgumentNullException(nameof(replies));
            _pollInterval = pollInterval;
            _logger = logger;

            Id = Guid.NewGuid();
        }

        private TimeSpan IdleLineDelay => TimeSpan.FromSeconds(1.0/_connection.BaudRate * 16.0);

        private bool IsPolling => _pollInterval > TimeSpan.Zero;

        /// <summary>
        /// Unique identifier of the bus
        /// </summary>
        public Guid Id { get; }

        public IEnumerable<byte> ConfigureDeviceAddresses => _configuredDevices.Select(device => device.Address);

        /// <summary>
        /// Closes down the connection
        /// </summary>
        public void Close()
        {
            _isShuttingDown = true;
            _connection.Close();
        }

        /// <summary>
        /// Send a command to a device
        /// </summary>
        /// <param name="command">Details about the command</param>
        public void SendCommand(Command command)
        {
            var foundDevice = _configuredDevices.First(device => device.Address == command.Address);
            
            foundDevice.SendCommand(command);
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
        public async Task StartPollingAsync()
        {
            DateTime lastMessageSentTime = DateTime.MinValue;

            while (!_isShuttingDown)
            {
                if (!_connection.IsOpen)
                {
                    try
                    {
                        _connection.Open();
                    }
                    catch (Exception exception)
                    {
                        _logger?.LogError(exception, "Error while opening connection");
                        foreach (var device in _configuredDevices.ToArray())
                        {
                            ResetDevice(device);
                            UpdateConnectionStatus(device);
                        }

                        await Task.Delay(TimeSpan.FromSeconds(5));

                        continue;
                    }
                }

                if (IsPolling)
                {
                    TimeSpan timeDifference = _pollInterval - (DateTime.UtcNow - lastMessageSentTime);
                    await Task.Delay(timeDifference > TimeSpan.Zero ? timeDifference : TimeSpan.Zero)
                        .ConfigureAwait(false);
                    
                    if (!_configuredDevices.Any())
                    {
                        lastMessageSentTime = DateTime.UtcNow;
                        continue;
                    }
                }
                else
                {
                    // Keep CPU usage down while waiting for next command
                    await Task.Delay(TimeSpan.FromMilliseconds(10));
                }

                foreach (var device in _configuredDevices.ToArray())
                {
                    // Right now it always sends sequence 0
                    if (!IsPolling)
                    {
                        device.MessageControl.ResetSequence();
                    }

                    // Requested delay for multi-messages
                    if (device.RequestDelay > DateTime.UtcNow)
                    {
                        continue;
                    }
                    
                    var command = device.GetNextCommandData(IsPolling);
                    if (command == null || WaitingForNextMultiMessage(command, device.IsSendingMultiMessage))
                    {
                        continue;
                    }
                    
                    lastMessageSentTime = DateTime.UtcNow;

                    Reply reply;
                    
                    try
                    {
                        // Reset the device if it loses connection
                        if (IsPolling && UpdateConnectionStatus(device) && !device.IsConnected)
                        {
                            ResetDevice(device);
                        }
                    }
                    catch (Exception exception)
                    {
                        _logger?.LogError(exception, $"Error while notifying connection status for address {command.Address}");
                    }

                    try
                    {
                        reply = await SendCommandAndReceiveReply(command, device).ConfigureAwait(false);
                        
                        // Prevent plain text message replies when secure channel has been established
                        if (device.UseSecureChannel && device.IsSecurityEstablished && !reply.IsSecureMessage)
                        {
                            _logger?.LogWarning("An plain text message was received when the secure channel had been established");
                            device.CreateNewRandomNumber();
                            ResetDevice(device);
                            continue;
                        }
                    }
                    catch (TimeoutException)
                    {
                        // Make sure the security is reset properly if reader goes offline
                        if (IsPolling && device.IsSecurityEstablished && !IsOnline(command.Address))
                        {
                            ResetDevice(device);
                        }
                        else if (IsPolling && device.UseSecureChannel)
                        {
                            device.CreateNewRandomNumber();
                        }

                        continue;
                    }
                    catch (Exception exception)
                    {
                        if (!_isShuttingDown)
                        {
                            _logger?.LogError(exception, $"Error while sending command {command} to address {command.Address}");
                        }
                        continue;
                    }

                    try
                    {
                        ProcessReply(reply, device);
                    }
                    catch (Exception exception)
                    {
                        _logger?.LogError(exception, $"Error while processing reply {reply} from address {reply.Address}");
                        continue;
                    }

                    await Task.Delay(IdleLineDelay).ConfigureAwait(false);
                }
            }
        }

        private static bool WaitingForNextMultiMessage(Command command, bool sendingMultiMessage)
        {
            return sendingMultiMessage && command is not FileTransferCommand;
        }

        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;

        private bool UpdateConnectionStatus(Device device)
        {
            bool isConnected = device.IsConnected;

            if (_lastConnectionStatus.ContainsKey(device.Address) &&
                _lastConnectionStatus[device.Address] == isConnected) return false;
            
            var handler = ConnectionStatusChanged;
            handler?.Invoke(this, new ConnectionStatusEventArgs(device.Address, isConnected));
                
            _lastConnectionStatus[device.Address] = isConnected;

            return true;
        }

        private void ProcessReply(Reply reply, Device device)
        {
            if (!reply.IsValidReply)
            {
                return;
            }

            if (reply.IsSecureMessage)
            {
                var mac = device.GenerateMac(reply.MessageForMacGeneration.ToArray(), false);
                if (!reply.IsValidMac(mac))
                {
                    // Appears that some legacy readers need a few seconds before attempting to reset
                    Thread.Sleep(TimeSpan.FromSeconds(5));
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
            
            if (reply.Type == ReplyType.Nak &&
                (device.UseSecureChannel && (reply.ExtractReplyData.First() == (byte) ErrorCode.DoesNotSupportSecurityBlock ||
                 reply.ExtractReplyData.First() == (byte) ErrorCode.CommunicationSecurityNotMet) ||
                 reply.ExtractReplyData.First() == (byte) ErrorCode.UnexpectedSequenceNumber))
            {
                if (reply.ExtractReplyData.First() == (byte) ErrorCode.UnexpectedSequenceNumber || reply.Sequence > 0) ResetDevice(device);
            }

            switch (reply.Type)
            {
                case ReplyType.CrypticData:
                    device.InitializeSecureChannel(reply);
                    break;
                case ReplyType.InitialRMac:
                    device.ValidateSecureChannelEstablishment(reply);
                    break;
            }

            _replies.Add(reply);
        }

        public void ResetDevice(int address)
        {
            var foundDevice = _configuredDevices.First(device => device.Address == address);
            
            ResetDevice(foundDevice);
        }

        public void SetSendingMultiMessage(byte address, bool isSendingMultiMessage)
        {
            var foundDevice = _configuredDevices.First(device => device.Address == address);

            foundDevice.IsSendingMultiMessage = isSendingMultiMessage;
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
            AddDevice(device.Address, device.MessageControl.UseCrc, device.UseSecureChannel, device.SecureChannelKey);
        }

        private async Task<Reply> SendCommandAndReceiveReply(Command command, Device device)
        {
            byte[] commandData;
            try
            {
                commandData = command.BuildCommand(device);
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception, $"Error while building command {command}");
                throw;
            }

            // _logger?.LogInformation($"Raw write data: {BitConverter.ToString(commandData)}", Id, command.Address);

            var buffer = new byte[commandData.Length + 1];
            buffer[0] = DriverByte;
            Buffer.BlockCopy(commandData, 0, buffer, 1, commandData.Length);
            
            await _connection.WriteAsync(buffer).ConfigureAwait(false);

            var replyBuffer = new Collection<byte>();

            if (!await WaitForStartOfMessage(replyBuffer).ConfigureAwait(false))
            {
                throw new TimeoutException("Timeout waiting for reply message");
            }

            if (!await WaitForMessageLength(replyBuffer).ConfigureAwait(false))             
            {
                throw new TimeoutException("Timeout waiting for reply message length");
            }

            if (!await WaitForRestOfMessage(replyBuffer, ExtractMessageLength(replyBuffer)).ConfigureAwait(false))
            {
                throw new TimeoutException("Timeout waiting for rest of reply message");
            }

            // _logger?.LogInformation($"Raw reply data: {BitConverter.ToString(replyBuffer.ToArray())}", Id,
            //      command.Address);

            return Reply.Parse(replyBuffer.ToArray(), Id, command, device);
        }

        private static ushort ExtractMessageLength(IReadOnlyList<byte> replyBuffer)
        {
            return Message.ConvertBytesToUnsignedShort(new[] {replyBuffer[2], replyBuffer[3]});
        }

        private async Task<bool> WaitForRestOfMessage(ICollection<byte> replyBuffer, ushort replyLength)
        {
            while (replyBuffer.Count < replyLength)
            {
                int maxReadBufferLength = _connection.BaudRate / 60;
                int remainingLength = replyLength - replyBuffer.Count;
                byte[] readBuffer = new byte[Math.Min(maxReadBufferLength, remainingLength)];
                int bytesRead = await TimeOutReadAsync(readBuffer, _connection.ReplyTimeout).ConfigureAwait(false);
                if (bytesRead > 0)
                {
                    for (byte index = 0; index < bytesRead; index++)
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

        private async Task<bool> WaitForMessageLength(ICollection<byte> replyBuffer)
        {
            while (replyBuffer.Count < 4)
            {
                byte[] readBuffer = new byte[4];
                int bytesRead = await TimeOutReadAsync(readBuffer, _connection.ReplyTimeout).ConfigureAwait(false);
                if (bytesRead > 0)
                {
                    for (byte index = 0; index < bytesRead; index++)
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

        private async Task<bool> WaitForStartOfMessage(ICollection<byte> replyBuffer)
        {
            while (true)
            {
                byte[] readBuffer = new byte[1];
                int bytesRead = await TimeOutReadAsync(readBuffer, _connection.ReplyTimeout).ConfigureAwait(false);
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

        private async Task<int> TimeOutReadAsync(byte[] buffer, TimeSpan timeout)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout);
            try
            {
                return await _connection.ReadAsync(buffer, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
            catch
            {
                if (!_isShuttingDown) throw;

                return 0;
            }
        }

        public class ConnectionStatusEventArgs
        {
            public ConnectionStatusEventArgs(byte address, bool isConnected)
            {
                Address = address;
                IsConnected = isConnected;
            }

            public byte Address { get; }
            public bool IsConnected { get; }
        }
    }
}