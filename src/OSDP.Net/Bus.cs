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

namespace OSDP.Net
{
    /// <summary>
    /// A group of OSDP devices sharing communications
    /// </summary>
    internal class Bus
    {
        private const byte DriverByte = 0xFF;
        private readonly SortedSet<Device> _configuredDevices = new SortedSet<Device>();
        private readonly object _configuredDevicesLock = new object();
        private readonly IOsdpConnection _connection;
        private readonly Dictionary<byte, bool> _lastConnectionStatus = new Dictionary<byte, bool>();

        private readonly ILogger<ControlPanel> _logger;
        private readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(250);

        private readonly TimeSpan _readTimeout = TimeSpan.FromMilliseconds(200);
        private readonly BlockingCollection<Reply> _replies;

        private bool _isShuttingDown;

        public Bus(IOsdpConnection connection, BlockingCollection<Reply> replies, ILogger<ControlPanel> logger = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _replies = replies ?? throw new ArgumentNullException(nameof(replies));
            _logger = logger;

            Id = Guid.NewGuid();
        }

        private TimeSpan IdleLineDelay => TimeSpan.FromSeconds(1.0/_connection.BaudRate * 16.0);

        /// <summary>
        /// Unique identifier of the bus
        /// </summary>
        public Guid Id { get; }

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

                var addedDevice = new Device(address, useCrc, useSecureChannel);
                if (secureChannelKey != null)
                {
                    addedDevice.UpdateSecureChannelKey(secureChannelKey);
                }
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
                        _logger?.LogError("Error while opening connection", exception);
                    }
                }

                TimeSpan timeDifference = _pollInterval - (DateTime.UtcNow - lastMessageSentTime);
                await Task.Delay(timeDifference > TimeSpan.Zero ? timeDifference : TimeSpan.Zero).ConfigureAwait(false);

                if (!_configuredDevices.Any())
                {
                    lastMessageSentTime = DateTime.UtcNow;
                    continue;
                }

                foreach (var device in _configuredDevices.ToArray())
                {
                    var data = new List<byte> {DriverByte};
                    var command = device.GetNextCommandData();
                    
                    lastMessageSentTime = DateTime.UtcNow;

                    Reply reply;
                    
                    try
                    {
                        UpdateConnectionStatus(device);
                    }
                    catch (Exception exception)
                    {
                        _logger?.LogError("Error while notifying connection status for address {command.Address}", exception);
                    }

                    try
                    {
                        reply = await SendCommandAndReceiveReply(data, command, device).ConfigureAwait(false);
                    }
                    catch (InvalidOperationException exception)
                    {
                        _logger?.LogWarning("Port is closed, reconnecting...", exception);
                        _connection.Close();
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        break;
                    }
                    catch (Exception exception)
                    {
                        _logger?.LogError($"Error while sending command {command} to address {command.Address}", exception);
                        continue;
                    }

                    try
                    {
                        ProcessReply(reply, device);
                    }
                    catch (Exception exception)
                    {
                        _logger?.LogError($"Error while processing reply {reply} from address {reply.Address}", exception);
                        continue;
                    }

                    await Task.Delay(IdleLineDelay).ConfigureAwait(false);
                }
            }
        }

        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;

        private void UpdateConnectionStatus(Device device)
        {
            bool isConnected = device.IsConnected;

            if (_lastConnectionStatus.ContainsKey(device.Address) &&
                _lastConnectionStatus[device.Address] == isConnected) return;
            
            var handler = ConnectionStatusChanged;
            handler?.Invoke(this, new ConnectionStatusEventArgs(device.Address, isConnected));
                
            _lastConnectionStatus[device.Address] = isConnected;
        }

        private void ProcessReply(Reply reply, Device device)
        {
            if (!reply.IsValidReply)
            {
                return;
            }

            if (reply.IsSecureMessage)
            {
                var mac = device.GenerateMac(reply.MessageForMacGeneration, false);
                if (!reply.IsValidMac(mac))
                {
                    device.ResetSecurity();
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
                (reply.ExtractReplyData.First() == (byte) ErrorCode.DoesNotSupportSecurityBlock ||
                 reply.ExtractReplyData.First() == (byte) ErrorCode.CommunicationSecurityNotMet))
            {
                device.ResetSecurity();
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

        private async Task<Reply> SendCommandAndReceiveReply(List<byte> data, Command command, Device device)
        {
            byte[] commandData;
            try
            {
                commandData = command.BuildCommand(device);
            }
            catch (Exception exception)
            {
                _logger?.LogError($"Error while building command {command}", exception);
                throw;
            }

            data.AddRange(commandData);

            _logger?.LogTrace($"Raw write data: {BitConverter.ToString(commandData)}", Id, command.Address);
            
            await _connection.WriteAsync(data.ToArray()).ConfigureAwait(false);

            var replyBuffer = new Collection<byte>();

            if (!await WaitForStartOfMessage(replyBuffer).ConfigureAwait(false))
            {
                throw new Exception("Timeout waiting for reply message");
            }

            if (!await WaitForMessageLength(replyBuffer).ConfigureAwait(false))             
            {
                throw new Exception("Timeout waiting for reply message length");
            }

            if (!await WaitForRestOfMessage(replyBuffer, ExtractMessageLength(replyBuffer)).ConfigureAwait(false))
            {
                throw new Exception("Timeout waiting for rest of reply message");
            }

            _logger?.LogTrace($"Raw reply data: {BitConverter.ToString(replyBuffer.ToArray())}", Id,
                command.Address);

            return Reply.Parse(replyBuffer, Id, command, device);
        }

        private static ushort ExtractMessageLength(IReadOnlyList<byte> replyBuffer)
        {
            return Message.ConvertBytesToShort(new[] {replyBuffer[2], replyBuffer[3]});
        }

        private async Task<bool> WaitForRestOfMessage(ICollection<byte> replyBuffer, ushort replyLength)
        {
            while (replyBuffer.Count < replyLength)
            {
                byte[] readBuffer = new byte[byte.MaxValue];
                int bytesRead = await TimeOutReadAsync(readBuffer).ConfigureAwait(false);
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
                int bytesRead = await TimeOutReadAsync(readBuffer).ConfigureAwait(false);
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
                int bytesRead = await TimeOutReadAsync(readBuffer).ConfigureAwait(false);
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

        private async Task<int> TimeOutReadAsync(byte[] buffer)
        {
            using (var cancellationTokenSource = new CancellationTokenSource(_readTimeout))
            {
                try
                {
                    return await _connection.ReadAsync(buffer, cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return 0;
                }
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