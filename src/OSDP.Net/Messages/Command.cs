using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OSDP.Net.Tests")]
namespace OSDP.Net.Messages
{
    public abstract class Command : Message
    {
        protected abstract byte CommandCode { get; }

        protected abstract IEnumerable<byte> SecurityControlBlock();

        protected abstract void CustomCommandUpdate(List<byte> commandBuffer);

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

            if (device.IsSecurityEstablished)
            {
                commandBuffer.AddRange(EncryptedData(device));
                
                // include mac and crc in length before generating mac
                AddPacketLength(commandBuffer, (ushort) (4 + (device.MessageControl.UseCrc ? 2 : 1)));

                commandBuffer.AddRange(device.GenerateMac(commandBuffer.ToArray(), true).Take(4));
            }
            else
            {
                commandBuffer.AddRange(Data());
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

            CustomCommandUpdate(commandBuffer);
            
            return commandBuffer.ToArray();
        }
    }
}