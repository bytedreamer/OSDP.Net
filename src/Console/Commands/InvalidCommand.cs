using System;
using OSDP.Net.Messages;

namespace Console.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public class InvalidCommand : Command
    {
        public InvalidCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => 0x59;

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
            return ReadOnlySpan<byte>.Empty;
        }
    }
}