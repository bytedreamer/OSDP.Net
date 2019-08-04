using System.Collections.Generic;
using System.Linq;

namespace OSDP.Net.Messages
{
    public abstract class Command : Message
    {
        protected abstract byte CommandCode { get; }

        public byte Address { get; protected set; }

        public bool UsingCrc { get; private set; }

        public bool Securing { get; private set; }

        protected abstract IEnumerable<byte> SecurityControlBlock();

        protected abstract IEnumerable<byte> Data();

        public byte[] BuildCommand(Control control)
        {
            UsingCrc = control.UseCrc;
            Securing = control.HasSecurityControlBlock;
            
            var command = new List<byte>
            {
                StartOfMessage,
                Address,
                0x0,
                0x0,
                control.ControlByte
            };

            if (Securing)
            {
                command.AddRange(SecurityControlBlock());
            }

            command.Add(CommandCode);
            
            command.AddRange(Data());

            command.Add(0x0);
            
            if (UsingCrc)
            {
                command.Add(0x0);
            }

            AddPacketLength(command);

            if (UsingCrc)
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