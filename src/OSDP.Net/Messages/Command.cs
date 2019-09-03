using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OSDP.Net.Tests")]
namespace OSDP.Net.Messages
{
    public abstract class Command : Message
    {
        protected abstract byte CommandCode { get; }

        public byte Address { get; protected set; }

        protected abstract IEnumerable<byte> SecurityControlBlock();

        protected abstract IEnumerable<byte> Data();

        internal byte[] BuildCommand(Device device)
        {
            var commandBuffer = new List<byte>
            {
                StartOfMessage,
                Address,
                0x0,
                0x0,
                device.MessageControl.ControlByte
            };

            if ( device.MessageControl.HasSecurityControlBlock)
            {
                commandBuffer.AddRange(SecurityControlBlock());
            }

            commandBuffer.Add(CommandCode);
            
            commandBuffer.AddRange(Data());
           
            if ( device.MessageControl.HasSecurityControlBlock && device.IsSecurityEstablished)
            {
                // include mac and crc in length before generating mac
                AddPacketLength(commandBuffer, (ushort)(4 + (device.MessageControl.UseCrc ? 2 : 1)));
                
                commandBuffer.AddRange(device.GenerateMac(commandBuffer.ToArray()).Take(4));
            }

            commandBuffer.Add(0x0);
            
            if (device.MessageControl.UseCrc)
            {
                commandBuffer.Add(0x0);
            }

            AddPacketLength(commandBuffer);

            if (device.MessageControl.UseCrc)
            {
                AddCrc(commandBuffer);
            }
            else
            {
                AddChecksum(commandBuffer);
            }
            
            return commandBuffer.ToArray();
        }

        internal static void AddPacketLength(IList<byte> command, ushort additionalLength = 0)
        {
            var packetLength = ConvertShortToBytes((ushort)(command.Count + additionalLength)).ToArray();
            command[2] = packetLength[0];
            command[3] = packetLength[1];
        }

        internal static void AddCrc(IList<byte> command)
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