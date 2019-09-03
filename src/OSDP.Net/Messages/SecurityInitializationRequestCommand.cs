using System;
using System.Collections.Generic;

namespace OSDP.Net.Messages
{
    public class SecurityInitializationRequestCommand : Command
    {
        private readonly byte[] _serverRandomNumber;

        internal SecurityInitializationRequestCommand(byte address, byte[] serverRandomNumber)
        {
            Address = address;
            _serverRandomNumber = serverRandomNumber ?? throw new ArgumentNullException(nameof(serverRandomNumber));
        }

        protected override byte CommandCode => 0x76;

        protected override IEnumerable<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x03,
                0x11,
                0x00
            };
        }

        protected override IEnumerable<byte> Data()
        {
            return _serverRandomNumber;
        }
    }
}