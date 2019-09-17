using System;
using System.Collections.Generic;

namespace OSDP.Net.Messages
{
    internal class AckReply : Reply
    {
        public AckReply()
        {
        }

        protected override byte ReplyCode => 0x40;

        protected override IEnumerable<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x02,
                0x16
            };
        }

        protected override IEnumerable<byte> Data()
        {
            return new byte[] { };
        }
    }
}