using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using OSDP.Net.Messages;

namespace OSDP.Net
{
    public class ControlPanel
    {
        private const byte StartOfMessage = 0x53;

        private readonly IOsdpConnection _connection;

        public ControlPanel(IOsdpConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public async Task<byte[]> SendCommand(CommandBase command)
        {
            if (!_connection.IsOpen)
            {
                _connection.Open();
            }

            var data = new List<byte> {0xFF};
            data.AddRange(command.BuildCommand(0, 
                new Control(0, true, false)));

            await _connection.Write(data.ToArray());

            var replyBuffer = new Collection<byte>();

            while (true)
            {
                byte[] readBuffer = new byte[1];
                int bytesRead = await _connection.Read(readBuffer);
                if (bytesRead == 0 || readBuffer[0] != StartOfMessage)
                {
                    continue;
                }

                replyBuffer.Add(readBuffer[0]);
                break;
            }

            while (replyBuffer.Count < 4)
            {
                byte[] readBuffer = new byte[1];
                int bytesRead = await _connection.Read(readBuffer);
                if (bytesRead == 1)
                {
                    replyBuffer.Add(readBuffer[0]);
                }
            }

            ushort replyLength = BitConverter.ToUInt16(new[] {replyBuffer[2], replyBuffer[3]}, 0);
            while (replyBuffer.Count < replyLength)
            {
                byte[] readBuffer = new byte[1];
                int bytesRead = await _connection.Read(readBuffer);
                if (bytesRead == 1)
                {
                    replyBuffer.Add(readBuffer[0]);
                }
            }

            return replyBuffer.ToArray();
        }

        public void Shutdown()
        {
            _connection.Close();
        }
    }
}