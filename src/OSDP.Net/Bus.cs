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
        private const byte StartOfMessage = 0x53;
        
        private readonly TimeSpan _readTimeout = TimeSpan.FromMilliseconds(200);
        private readonly IOsdpConnection _connection;

        public Bus(IOsdpConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public async Task StartPollingAsync(byte address, CancellationToken cancellationToken)
        {
            byte sequence = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_connection.IsOpen)
                {
                    _connection.Open();
                }

                if (sequence > 3)
                {
                    sequence = 1;
                }

                var data = new List<byte> {0xFF};
                data.AddRange(new PollCommand().BuildCommand(address,
                    new Control(sequence++, true, false)));

                await _connection.WriteAsync(data.ToArray());

                var replyBuffer = new Collection<byte>();

                while (true)
                {
                    byte[] readBuffer = new byte[1];
                    int bytesRead = await TimeOutReadAsync(readBuffer);
                    if (bytesRead == 0)
                    {
                        return;
                    }

                    if (readBuffer[0] != StartOfMessage)
                    {
                        continue;
                    }

                    replyBuffer.Add(readBuffer[0]);
                    break;
                }

                while (replyBuffer.Count < 4)
                {
                    byte[] readBuffer = new byte[1];
                    int bytesRead = await TimeOutReadAsync(readBuffer);
                    if (bytesRead == 1)
                    {
                        replyBuffer.Add(readBuffer[0]);
                    }
                    else
                    {
                        return;
                    }
                }

                ushort replyLength = BitConverter.ToUInt16(new[] {replyBuffer[2], replyBuffer[3]}, 0);
                while (replyBuffer.Count < replyLength)
                {
                    byte[] readBuffer = new byte[1];
                    int bytesRead = await TimeOutReadAsync(readBuffer);
                    if (bytesRead == 1)
                    {
                        replyBuffer.Add(readBuffer[0]);
                    }
                    else
                    {
                        return;
                    }
                }

                Console.WriteLine(BitConverter.ToString(replyBuffer.ToArray()));

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        
        public void Close()
        {
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