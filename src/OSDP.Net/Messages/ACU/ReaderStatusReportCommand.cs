using System;

namespace OSDP.Net.Messages.ACU
{
    internal class ReaderStatusReportCommand : Command
    {
        public ReaderStatusReportCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => 0x67;

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