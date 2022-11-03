using OSDP.Net.Messages;
using OSDP.Net.Model.ReplyData;
using System;

namespace OSDP.Net
{
    /// <summary>
    /// Represents a custom exception defined in OSDP.Net library
    /// </summary>
    public class OSDPNetException : Exception 
    {
        /// <summary>
        /// Initializes a new instance of OSDP.Net.OSDPNetException
        /// </summary>
        public OSDPNetException() : base() { }

        /// <summary>
        /// Initializes a new instance of OSDP.Net.OSDPNetException with a specified
        /// error message
        /// </summary>
        /// <param name="message">Message that describes the error</param>
        public OSDPNetException(string message) : base(message) {}
    }

    /// <summary>
    /// Represent a failure condition where PD indicates that it didn't accept the 
    /// command by replying with osdp_NAK packet
    /// </summary>
    public class NackReplyException : OSDPNetException
    {
        /// <summary>
        /// Parsed osdp_NAK packet data
        /// </summary>
        public Nak Reply { get; }

        /// <summary>
        /// Initializes a new instance of OSDP.Net.NackReplyException class
        /// </summary>
        /// <param name="replyData">osdp_NAK packet data returned from PD</param>
        /// <param name="message">Optional message to be included with the exception</param>
        public NackReplyException(Nak replyData, string message = null) : base(message)
        {
            Reply = replyData;
        }
    }
}