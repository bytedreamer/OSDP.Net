using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OSDP.Net.Connections;
using OSDP.Net.Messages;
using OSDP.Net.Messages.ACU;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model.ReplyData;
using OutgoingMessage = OSDP.Net.Messages.OutgoingMessage;

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

            var controlBlock = new Control((byte)(buffer[4] & 0x03), true, false);
            var outgoingMessage = new OutgoingMessage(0, controlBlock, new Ack());
            await _stream.WriteAsync(outgoingMessage.BuildMessage(new PdMessageSecureChannelBase()));

            _stream.Position = 0;
        }

        public async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
        {
            return await _stream.ReadAsync(buffer, 0, buffer.Length, token);
        }
    }
}