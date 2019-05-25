using System.Collections.Generic;
using System.Linq;

namespace OSDP.Net.Messages
{
    public abstract class CommandBase : Message
    {
        protected abstract byte CommandCode { get; }
        
        public byte[] BuildCommand(byte address, Control control)
        {
            var command = new List<byte>
            {
                StartOfMessage,
                address,
                0x0,
                0x0,
                control.ControlByte,
                CommandCode,
                0x0
            };

            if (control.UseCrc)
            {
                command.Add(0x0);
            }

            AddPacketLength(command);

            if (control.UseCrc)
            {
                AddCrc(command);
            }
            else
            {
                AddChecksum(command);
            }

            return command.ToArray();
        }
        
        private static void AddPacketLength(IList<byte> command)
        {
            var packetLength = ConvertShortToBytes((ushort)command.Count);
            command[2] = packetLength[0];
            command[3] = packetLength[1];
        }

        private static void AddCrc(IList<byte> command)
        {
            ushort crc = CalculateCrc(command.Take(command.Count - 2).ToArray());
            var crcBytes = ConvertShortToBytes(crc);
            command[command.Count - 2] = crcBytes[0];
            command[command.Count - 1] = crcBytes[1];
        }

        private static void AddChecksum(IList<byte> command)
        {
            command[command.Count - 1] =
                (byte) (0x100 - command.Aggregate(0, (source, element) => source + element) & 0xff);
        }
    }
}