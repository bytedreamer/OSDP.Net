using System;

namespace OSDP.Net.Messages.ACU
{
    internal class ServerCryptogramCommand : Command
    {
        private readonly bool _isDefaultKey;
        private readonly byte[] _serverCryptogram;

        internal ServerCryptogramCommand(byte address, byte[] serverCryptogram, bool isDefaultKey)
        {
            Address = address;
            _serverCryptogram = serverCryptogram ?? throw new ArgumentNullException(nameof(serverCryptogram));
            _isDefaultKey = isDefaultKey;
        }

        protected override byte CommandCode => (byte)CommandType.ServerCryptogram;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x03,
                (byte)SecurityBlockType.SecureConnectionSequenceStep3,
                (byte)(_isDefaultKey ? 0x00 : 0x01)
            };
        }

        protected override ReadOnlySpan<byte> Data()
        {
            return _serverCryptogram;
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }
    }
}