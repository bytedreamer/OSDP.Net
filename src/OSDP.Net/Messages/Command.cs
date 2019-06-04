using System.Collections.Generic;
using System.Linq;

namespace OSDP.Net.Messages
{
    public abstract class Command : Message
    {
        protected abstract byte CommandCode { get; }
        
        protected abstract byte Address { get; }
        
        public abstract Control Control { get; }
        
        public byte[] BuildCommand()
        {
            var command = new List<byte>
            {
                StartOfMessage,
                Address,
                0x0,
                0x0,
                Control.ControlByte,
                CommandCode,
                0x0
            };

            if (Control.UseCrc)
            {
                command.Add(0x0);
            }

            AddPacketLength(command);

            if (Control.UseCrc)
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
            var packetLength = ConvertShortToBytes((ushort)command.Count).ToArray();
            command[2] = packetLength[0];
            command[3] = packetLength[1];
        }

        private static void AddCrc(IList<byte> command)
        {
            ushort crc = CalculateCrc(command.Take(command.Count - 2).ToArray());
            var crcBytes = ConvertShortToBytes(crc).ToArray();
            command[command.Count - 2] = crcBytes[0];
            command[command.Count - 1] = crcBytes[1];
        }

        private static void AddChecksum(IList<byte> command)
        {
            command[command.Count - 1] = CalculateChecksum(command.Take(command.Count - 1).ToArray());
        }
    }
}