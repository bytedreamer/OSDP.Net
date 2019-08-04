using System;
using System.Collections.Generic;

namespace OSDP.Net.Messages
{
    public class SecurityInitializationRequestCommand : Command
    {
        private readonly byte[] _randomBytes = new byte[8];

        public SecurityInitializationRequestCommand(byte address)
        {
            Address = address;
            new Random().NextBytes(_randomBytes);
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
            return _randomBytes;
        }
    }
}