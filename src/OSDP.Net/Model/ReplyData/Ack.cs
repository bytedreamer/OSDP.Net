using OSDP.Net.Messages;
using System;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// Generic ACK reply
    /// </summary>
    public class Ack : PayloadData
    {
        /// <inheritdoc/>
        public override byte Code => (byte)ReplyType.Ack;

        /// <inheritdoc/>
        public override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.ReplyMessageWithNoDataSecurity;
        }

        /// <inheritdoc/>
        public override byte[] BuildData()
        {
            return Array.Empty<byte>();
        }
    }
}
