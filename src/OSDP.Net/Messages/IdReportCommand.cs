using System;

namespace OSDP.Net.Messages
{
    internal class IdReportCommand : Command
    {
        public IdReportCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => 0x61;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x02,
                0x17
            };
        }

        protected override ReadOnlySpan<byte> Data()
        {
            return new byte[] { 0x00 };
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }
    }
}