using System;
using System.Collections.Generic;

namespace OSDP.Net.Messages.ACU
{
    internal class AckReply : Reply
    {
        protected override byte ReplyCode => (byte)ReplyType.Ack;

        protected override IEnumerable<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x02,
                0x16
            };
        }

        protected override ReadOnlySpan<byte> Data()
        {
            return ReadOnlySpan<byte>.Empty;
        }
    }
}