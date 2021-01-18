using System;

namespace OSDP.Net.Messages
{
    internal class SecurityInitializationRequestCommand : Command
    {
        private readonly byte[] _serverRandomNumber;

        internal SecurityInitializationRequestCommand(byte address, byte[] serverRandomNumber)
        {
            Address = address;
            _serverRandomNumber = serverRandomNumber ?? throw new ArgumentNullException(nameof(serverRandomNumber));
        }

        protected override byte CommandCode => 0x76;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x03,
                0x11,
                0x00
            };
        }

        protected override ReadOnlySpan<byte> Data()
        {
            return _serverRandomNumber;
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
            
        }
    }
}