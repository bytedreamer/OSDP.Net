using System;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Messages.ACU
{
    internal class KeepReaderActiveCommand : Command
    {
        private readonly ushort _keepAliveTimeInMilliseconds;

        public KeepReaderActiveCommand(byte address, ushort keepAliveTimeInMilliseconds)
        {
            Address = address;
            _keepAliveTimeInMilliseconds = keepAliveTimeInMilliseconds;
        }

        protected override byte CommandCode => (byte)CommandType.KeepActive;

        protected override ReadOnlySpan<byte> Data()
        {
            return ConvertShortToBytes(_keepAliveTimeInMilliseconds);
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