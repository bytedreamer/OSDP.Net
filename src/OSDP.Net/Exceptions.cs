using OSDP.Net.Messages;
using OSDP.Net.Model.ReplyData;
using System;

namespace OSDP.Net
{
    /// <summary>
    /// 
    /// </summary>
    public class OSDPNetException : Exception { }

    /// <summary>
    /// 
    /// </summary>
    public class CommandException : OSDPNetException { }

    /// <summary>
    /// 
    /// </summary>
    public class NackReplyException : CommandException
    {
        /// <summary>
        /// 
        /// </summary>
        public Nak Reply { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="replyData"></param>
        public NackReplyException(Nak replyData) 
        {
            Reply = replyData;
        }
    }
}