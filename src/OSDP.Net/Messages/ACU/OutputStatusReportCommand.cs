using System;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Messages.ACU
{
    internal class OutputStatusReportCommand : Command
    {
        public OutputStatusReportCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => (byte)CommandType.OutputStatus;

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