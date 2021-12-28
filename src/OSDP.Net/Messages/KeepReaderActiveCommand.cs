using System;

namespace OSDP.Net.Messages
{
    internal class KeepReaderActiveCommand : Command
    {
        private readonly ushort _keepAliveTimeInMilliseconds;

        public KeepReaderActiveCommand(byte address, ushort keepAliveTimeInMilliseconds)
        {
            Address = address;
            _keepAliveTimeInMilliseconds = keepAliveTimeInMilliseconds;
        }

        protected override byte CommandCode => 0xA7;

        protected override ReadOnlySpan<byte> Data()
        {
            return Message.ConvertShortToBytes(_keepAliveTimeInMilliseconds);
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