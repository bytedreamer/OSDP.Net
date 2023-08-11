using System;
using Microsoft.Extensions.Logging;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Messages.ACU;

/// <summary>
/// Message channel which represents the Access Control Unit (ACU) side of the OSDP 
/// communications (i.e. OSDP commands are sent out and replies are received)
/// </summary>
internal class ACUMessageSecureChannel : MessageSecureChannel
{
    /// <summary>
    /// Initializes a new instance of the ACUMessageChannel
    /// </summary>
    /// <param name="context">Optional security context state to be used by the channel. If one 
    /// is not provided, new default instance of the context will be created internally. This is
    /// useful when more than one channel have a need to share the same security state (i.e. in
    /// cases of implementing a spy that analyzes traffic flow through the two inbound and outbound
    /// channels</param>
    /// <param name="loggerFactory">Optional logger factory from which a logger object for the
    /// message channel will be acquired</param>
    public ACUMessageSecureChannel(SecurityContext context = null, ILoggerFactory loggerFactory = null)
        : base(context, loggerFactory) {}

    /// <inheritdoc />
    public override void EncodePayload(byte[] payload, Span<byte> destination)
    {
        EncodePayload(payload, Context.RMac, destination);
    }

    /// <inheritdoc />
    public override byte[] DecodePayload(byte[] payload) => DecodePayload(payload, Context.CMac);

    /// <inheritdoc />
    public override ReadOnlySpan<byte> GenerateMac(ReadOnlySpan<byte> message, bool isIncoming) =>
        isIncoming ? GenerateReplyMac(message) : GenerateCommandMac(message);
}