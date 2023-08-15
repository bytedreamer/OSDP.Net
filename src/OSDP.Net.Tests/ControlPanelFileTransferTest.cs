using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OSDP.Net.Connections;
using OSDP.Net.Messages.ACU;

namespace OSDP.Net.Tests;

[TestFixture]
public class ControlPanelFileTransferTest
{
    // ReSharper disable once UnusedType.Local
    class TestConnection : IOsdpConnection
    {
        private readonly MemoryStream _stream = new MemoryStream();

        public int NumberOfTimesCalledClose { get; private set; }

        public int NumberOfTimesCalledOpen { get; private set; }

        public int BaudRate => 9600;

        public bool IsOpen => true;

        public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromSeconds(1);

        public void Open()
        {
            NumberOfTimesCalledOpen++;
        }

        public void Close()
        {
            NumberOfTimesCalledClose++;
        }

        public async Task WriteAsync(byte[] buffer)
        {
            await _stream.FlushAsync();

            await _stream.WriteAsync(
                new AckReply().BuildReply(0, 
                    new Control((byte) (buffer[4] & 0x03), true, false)));

            _stream.Position = 0;
        }

        public async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
        {
            return await _stream.ReadAsync(buffer, 0, buffer.Length, token);
        }
    }
}