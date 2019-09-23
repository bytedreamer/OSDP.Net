using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDP.Net.Messages
{
    internal class UnknownReply : Reply
    {
        public UnknownReply(IReadOnlyList<byte> data, Guid connectionId, Command issuingCommand, Device device) : base(
            data, connectionId, issuingCommand, device)
        {

        }

        protected override byte ReplyCode => (byte)Type;

        protected override IEnumerable<byte> Data()
        {
            return ExtractReplyData;
        }

        protected override IEnumerable<byte> SecurityControlBlock()
        {
            byte securityBlockLength = (byte)(SecureBlockData.Count() + 2);
            var securityControlBlock = new List<byte> {SecurityBlockType, securityBlockLength};
            securityControlBlock.AddRange(SecureBlockData);
            return securityControlBlock;
        }
    }
}