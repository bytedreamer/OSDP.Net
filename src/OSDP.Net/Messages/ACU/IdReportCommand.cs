using System;

namespace OSDP.Net.Messages.ACU
{
    internal class IdReportCommand : Command
    {
        public IdReportCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => (byte)CommandType.IdReport;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithDataSecurity;
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