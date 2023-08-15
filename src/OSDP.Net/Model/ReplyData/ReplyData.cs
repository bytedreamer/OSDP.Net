using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// Base class representing a payload of a PD reply message
    /// </summary>
    public abstract class ReplyData : PayloadData
    {
        /// <summary>
        /// Message reply code
        /// </summary>
        public abstract ReplyType ReplyType { get; }
    }
}
