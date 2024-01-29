using System;
using Microsoft.Extensions.Logging;

namespace OSDP.Net.Messages.SecureChannel
{
    /// <summary>
    /// Message channel which represents the Periphery Device (PD) side of the OSDP 
    /// communications (i.e. OSDP commands are received and replies are sent out)
    /// </summary>
    internal class PdMessageSecureChannel : MessageSecureChannel
    {
        /// <summary>
        /// Initializes a new instance of the PDMessageChannel
        /// </summary>
        /// <param name="context">Optional security context state to be used by the channel. If one 
        /// is not provided, new default instance of the context will be created internally. This is
        /// useful when more than one channel have a need to share the same security state (i.e. in
        /// cases of implementing a spy that analyzes traffic flow through the two inbound and outbound
        /// channels</param>
        /// <param name="loggerFactory">Optional logger factory from which a logger object for the
        /// message channel will be acquired</param>
        public PdMessageSecureChannel(SecurityContext context = null, ILoggerFactory loggerFactory = null) 
            : base(context, loggerFactory) {}

        /// <inheritdoc />
        public override void EncodePayload(byte[] payload, Span<byte> destination) =>
            EncodePayload(payload, Context.CMac, destination);

        /// <inheritdoc />
        public override byte[] DecodePayload(byte[] payload) => DecodePayload(payload, Context.RMac);

        /// <inheritdoc />
        public override ReadOnlySpan<byte> GenerateMac(ReadOnlySpan<byte> message, bool isIncoming) =>
            isIncoming ? GenerateCommandMac(message) : GenerateReplyMac(message);
    }
}
