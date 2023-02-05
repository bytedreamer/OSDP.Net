using System;
using OSDP.Net.Messages.ACU;

namespace Console.Commands
{
    /// <summary>
    /// Change the length on a poll command
    /// </summary>
    public class InvalidLengthPollCommand : Command
    {
        public InvalidLengthPollCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => 0x60;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x02,
                0x15
            };
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }

        protected override ReadOnlySpan<byte> Data()
        {
            return new byte[] { 0x01 };
        }
    }
}