using System;

namespace OSDP.Net.Messages
{
    internal class ACUReceiveSizeCommand : Command
    {
        private readonly ushort _maximumReceiveBuffer;

        public ACUReceiveSizeCommand(byte address, ushort maximumReceiveBuffer)
        {
            Address = address;
            _maximumReceiveBuffer = maximumReceiveBuffer;
        }

        protected override byte CommandCode => 0x7B;

        protected override ReadOnlySpan<byte> Data()
        {
            return ConvertShortToBytes(_maximumReceiveBuffer);
        }

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x02,
                0x17
            };
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }
    }
}