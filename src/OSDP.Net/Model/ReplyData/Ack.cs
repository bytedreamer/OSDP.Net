using OSDP.Net.Messages;
using System;

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
        public override byte[] BuildData()
        {
            return Array.Empty<byte>();
        }
    }
}
