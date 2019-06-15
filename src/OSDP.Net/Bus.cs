using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OSDP.Net.Messages;

namespace OSDP.Net
{
    public class Bus
    {
        private const byte DriverByte = 0xFF;
        private readonly SortedSet<Device> _configuredDevices = new SortedSet<Device>();
        private readonly IOsdpConnection _connection;

        private readonly TimeSpan _readTimeout = TimeSpan.FromMilliseconds(200);
        private readonly BlockingCollection<Reply> _replies;

        private bool _isShuttingDown;

        public Bus(IOsdpConnection connection, BlockingCollection<Reply> replies)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _replies = replies ?? throw new ArgumentNullException(nameof(replies));
        }

        public void Close()
        {
            _isShuttingDown = true;
            _connection.Close();
        }

        public async Task StartPollingAsync()
        {
            _configuredDevices.Add(new Device(0));
            DateTime lastMessageSentTime = DateTime.MinValue;

            while (!_isShuttingDown)
            {
                if (!_connection.IsOpen)
                {
                    _connection.Open();
                }
                
                TimeSpan timeDifference = TimeSpan.FromSeconds(1) - (DateTime.UtcNow - lastMessageSentTime);
                await Task.Delay(timeDifference > TimeSpan.Zero ? timeDifference : TimeSpan.Zero);
                
                var data = new List<byte> {DriverByte};
                var command = _configuredDevices.First().GetNextCommandData();
                var commandData = command.BuildCommand();
                data.AddRange(commandData);

                Console.WriteLine($"Raw write data: {BitConverter.ToString(commandData)}");
                
                lastMessageSentTime = DateTime.UtcNow;
                
                await _connection.WriteAsync(data.ToArray());
                
                var replyBuffer = new Collection<byte>();

                if (!await WaitForStartOfMessage(replyBuffer)) continue;

                if (!await WaitForMessageLength(replyBuffer)) continue;

                if (!await WaitForRestOfMessage(replyBuffer, ExtractMessageLength(replyBuffer))) continue;

                var reply = new Reply(replyBuffer);
                 
                if (!reply.IsValidReply(command)) continue;
                
                // ** Determine correct device to send reply received notice **
                
                _configuredDevices.First().ValidReplyHasBeenReceived(reply);
                
                _replies.Add(reply);
                
                // ** Idle delay needs to be added **

                Console.WriteLine($"Raw reply data: {BitConverter.ToString(replyBuffer.ToArray())}");
            }
        }

        private static ushort ExtractMessageLength(IReadOnlyList<byte> replyBuffer)
        {
            return BitConverter.ToUInt16(new[] {replyBuffer[2], replyBuffer[3]}, 0);
        }

        private async Task<bool> WaitForRestOfMessage(ICollection<byte> replyBuffer, ushort replyLength)
        {
            while (replyBuffer.Count < replyLength)
            {
                byte[] readBuffer = new byte[byte.MaxValue];
                int bytesRead = await TimeOutReadAsync(readBuffer);
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
                int bytesRead = await TimeOutReadAsync(readBuffer);
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
                int bytesRead = await TimeOutReadAsync(readBuffer);
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
                    return await _connection.ReadAsync(buffer, cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    return 0;
                }
            }
        }
    }
}