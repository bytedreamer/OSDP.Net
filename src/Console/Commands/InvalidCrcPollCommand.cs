using System.Collections.Generic;
using OSDP.Net.Messages;

namespace Console.Commands
{
    public class InvalidCrcPollCommand : Command
    {
        public InvalidCrcPollCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => 0x60;

        protected override IEnumerable<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x02,
                0x15
            };
        }

        protected override void CustomCommandUpdate(List<byte> commandBuffer)
        {
            commandBuffer[^1] = (byte)(commandBuffer[^1] + 1);
        }

        protected override IEnumerable<byte> Data()
        {
            return new byte[] { };
        }
    }
}