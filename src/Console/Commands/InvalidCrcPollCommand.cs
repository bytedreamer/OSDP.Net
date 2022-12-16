using System;
using OSDP.Net.Messages.ACU;

namespace Console.Commands
{
    /// <summary>
    /// Change the CRC on a poll command
    /// </summary>
    public class InvalidCrcPollCommand : Command
    {
        public InvalidCrcPollCommand(byte address)
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
            commandBuffer[^1] = (byte)(commandBuffer[^1] + 1);
        }

        protected override ReadOnlySpan<byte> Data()
        {
            return ReadOnlySpan<byte>.Empty;
        }
    }
}