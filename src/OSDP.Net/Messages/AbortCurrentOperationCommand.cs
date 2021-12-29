using System;

namespace OSDP.Net.Messages
{
    internal class AbortCurrentOperationCommand : Command
    {
        public AbortCurrentOperationCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => 0xA2;

        protected override ReadOnlySpan<byte> Data()
        {
            return ReadOnlySpan<byte>.Empty;
        }

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
    }
}