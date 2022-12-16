using OSDP.Net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSDP.Net.Model.ReplyData
{
    public class Ack : ReplyData
    {
        public override ReplyType ReplyType => ReplyType.Ack;

        public override byte[] BuildData(bool withPadding)
        {
            return Array.Empty<byte>();
        }
    }
}
