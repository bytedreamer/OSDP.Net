using System;
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
        
        private readonly TimeSpan _readTimeout = TimeSpan.FromMilliseconds(200);
        private readonly IOsdpConnection _connection;
        private readonly SortedSet<Device> _configuredDevices = new SortedSet<Device>();

        private bool _isShuttingDown;

        public Bus(IOsdpConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public async Task StartPollingAsync(CancellationToken cancellationToken)
        {
            _configuredDevices.Add(new Device(0));

            while (!_isShuttingDown)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_connection.IsOpen)
                {
                    _connection.Open();
                }

                var data = new List<byte> {DriverByte};
                data.AddRange(_configuredDevices.First().GetNextCommandData());

                await _connection.WriteAsync(data.ToArray());

                var replyBuffer = new Collection<byte>();

                if (!await WaitForStartOfMessage(replyBuffer)) continue;

                if (!await WaitForMessageLength(replyBuffer)) continue;

                if (!await WaitForRestOfMessage(replyBuffer, ExtractMessageLength(replyBuffer))) continue;

                Console.WriteLine(BitConverter.ToString(replyBuffer.ToArray()));

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        private static ushort ExtractMessageLength(Collection<byte> replyBuffer)
        {
            return BitConverter.ToUInt16(new[] {replyBuffer[2], replyBuffer[3]}, 0);
        }

        private async Task<bool> WaitForRestOfMessage(Collection<byte> replyBuffer, ushort replyLength)
        {
            while (replyBuffer.Count < replyLength)
            {
                byte[] readBuffer = new byte[sizeof(byte)];
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

        public void Close()
        {
            _isShuttingDown = true;
            _connection.Close();
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