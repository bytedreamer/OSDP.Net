using System;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Messages.ACU
{
    internal class AbortCurrentOperationCommand : Command
    {
        public AbortCurrentOperationCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => (byte)CommandType.Abort;

        protected override ReadOnlySpan<byte> Data()
        {
            return ReadOnlySpan<byte>.Empty;
        }

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithNoDataSecurity;
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }
    }
}