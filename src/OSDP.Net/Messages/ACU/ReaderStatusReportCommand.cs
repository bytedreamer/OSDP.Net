using System;

namespace OSDP.Net.Messages.ACU
{
    internal class ReaderStatusReportCommand : Command
    {
        public ReaderStatusReportCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => (byte)CommandType.ReaderStatus;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithNoDataSecurity;
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