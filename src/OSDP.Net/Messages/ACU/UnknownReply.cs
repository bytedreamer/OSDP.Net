using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDP.Net.Messages.ACU
{
    internal class UnknownReply : Reply
    {
        public UnknownReply(ReadOnlySpan<byte> data, Guid connectionId, Command issuingCommand, Device device) : base(
            data, connectionId, issuingCommand, device)
        {
        }

        protected override byte ReplyCode => (byte)Type;

        protected override ReadOnlySpan<byte> Data()
        {
            return ExtractReplyData.ToArray();
        }

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            byte securityBlockLength = (byte)(SecureBlockData.Count() + 2);
            var securityControlBlock = new List<byte> { SecurityBlockType, securityBlockLength };
            securityControlBlock.AddRange(SecureBlockData);
            return securityControlBlock.ToArray();
        }
    }
}
