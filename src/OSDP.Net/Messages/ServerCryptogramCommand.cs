using System;
using System.Collections.Generic;

namespace OSDP.Net.Messages
{
    public class ServerCryptogramCommand : Command
    {
        private readonly byte[] _serverCryptogram;

        internal ServerCryptogramCommand(byte address, byte[] serverCryptogram)
        {
            Address = address;
            _serverCryptogram = serverCryptogram ?? throw new ArgumentNullException(nameof(serverCryptogram));
        }

        protected override byte CommandCode => 0x77;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x03,
                0x13,
                0x00
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