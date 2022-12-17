using OSDP.Net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// Generic ACK reply
    /// </summary>
    public class Ack : ReplyData
    {
        /// <inheritdoc/>
        public override ReplyType ReplyType => ReplyType.Ack;

        /// <inheritdoc/>
        public override byte[] BuildData(bool withPadding)
        {
            return Array.Empty<byte>();
        }
    }
}
