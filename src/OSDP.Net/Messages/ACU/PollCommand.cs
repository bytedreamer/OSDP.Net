using System;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Messages.ACU
{
    internal class PollCommand : Command
    {
        public PollCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => (byte)CommandType.Poll;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithNoDataSecurity;
        }

        protected override ReadOnlySpan<byte> Data()
        {
            return ReadOnlySpan<byte>.Empty;
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }
    }
}