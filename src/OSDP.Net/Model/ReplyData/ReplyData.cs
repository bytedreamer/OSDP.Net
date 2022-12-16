using OSDP.Net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSDP.Net.Model.ReplyData
{
    public abstract class ReplyData
    {
        public abstract byte[] BuildData(bool withPadding = false);

        public abstract ReplyType ReplyType { get; }
    }
}
