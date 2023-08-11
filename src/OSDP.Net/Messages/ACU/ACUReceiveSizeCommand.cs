using System;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Messages.ACU
{
    internal class ACUReceiveSizeCommand : Command
    {
        private readonly ushort _maximumReceiveBuffer;

        public ACUReceiveSizeCommand(byte address, ushort maximumReceiveBuffer)
        {
            Address = address;
            _maximumReceiveBuffer = maximumReceiveBuffer;
        }

        protected override byte CommandCode => (byte)CommandType.MaxReplySize;

        protected override ReadOnlySpan<byte> Data()
        {
            return ConvertShortToBytes(_maximumReceiveBuffer);
        }

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithDataSecurity;
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }
    }
}