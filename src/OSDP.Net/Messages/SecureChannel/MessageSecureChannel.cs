using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace OSDP.Net.Messages.SecureChannel;

/// <summary>
/// V2 implementation of a SecureChannel class.
/// 
/// With introduction of OSDP PD simulator as well as CLI tool to parse pcap files generated 
/// by this library, we had to do some refactoring. In doing so, we introduced a new Message class
/// hierarchy based on Message -> IncomingMessage inheritance  (we are yet to add OutgoingMessage
/// but most of the logic that would go in that, is presently in OSDP.NET.Messages.PD.Reply class)
/// 
/// Whereas the older SecureChannel class was passed directly into Message parsing/building code,
/// this new class hierarchy depends on IMessageChannel interface to interact with the secure
/// channel context and this class is the base implementation for that
/// </summary>
internal abstract class MessageSecureChannel : IMessageSecureChannel
{
    /// <summary>
    /// Optional logger instance
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Initializes a new instance of SecurityChannel2 class
    /// </summary>
    /// <param name="context">Optional security context state to be used by the channel. If one 
    /// is not provided, new default instance of the context will be created internally. This is
    /// useful when more than one channel have a need to share the same security state (i.e. in
    /// cases of implementing a spy that analyzes traffic flow through the two inbound and outbound
    /// channels</param>
    /// <param name="loggerFactory">Optional logger factory from which a logger object for the
    /// message channel will be acquired</param>
    protected MessageSecureChannel(SecurityContext context = null, ILoggerFactory loggerFactory = null)
    {
        Context = context ?? new();
        Logger = loggerFactory?.CreateLogger(GetType());
    }

    /// <summary>
    /// Security state used by the channel
    /// </summary>
    protected SecurityContext Context { get; }

    /// <inheritdoc/>
    public bool IsSecurityEstablished => Context.IsSecurityEstablished;

    /// <inheritdoc/>
    public abstract byte[] DecodePayload(byte[] payload);

    /// <inheritdoc/>
    public abstract void EncodePayload(byte[] payload, Span<byte> destination);

    /// <inheritdoc/>
    public abstract ReadOnlySpan<byte> GenerateMac(ReadOnlySpan<byte> message, bool isIncoming);

    /// <summary>
    /// Generates a MAC for a command message
    /// </summary>
    /// <param name="message">Message bytes to generate code from</param>
    /// <returns>Newly generated MAC</returns>
    /// <exception cref="SecureChannelRequired">Thrown if secure channel has not been established</exception>
    protected byte[] GenerateCommandMac(ReadOnlySpan<byte> message) => Context.CMac = GenerateMac(message, Context.RMac);

    /// <summary>
    /// Generates a MAC for a reply message
    /// </summary>
    /// <param name="message">Message bytes to generate code from</param>
    /// <returns>Newly generated MAC</returns>
    /// <exception cref="SecureChannelRequired">Thrown if secure channel has not been established</exception>
    protected byte[] GenerateReplyMac(ReadOnlySpan<byte> message) => Context.RMac = GenerateMac(message, Context.CMac);

    private byte[] GenerateMac(ReadOnlySpan<byte> message, byte[] iv)
    {
        if (!IsSecurityEstablished)
        {
            throw new SecureChannelRequired();
        }

        using var crypto = SecurityContext.CreateCypher(Context.SMac1, false);
        crypto.IV = iv;
            
        var cursor = message;
        while (cursor.Length > 0)
        {
            byte[] block;

            if (cursor.Length < 16)
            {
                block = new byte[16];
                cursor.CopyTo(block);
                block[cursor.Length] = Message.FirstPaddingByte;
                cursor = cursor.Slice(cursor.Length);
                crypto.Key = Context.SMac2;
            }
            else
            {
                block = cursor.Slice(0, 16).ToArray();
                cursor = cursor.Slice(16);
                if (cursor.Length == 0) crypto.Key = Context.SMac2;
            }

            using var encryptor = crypto.CreateEncryptor();
            crypto.IV = encryptor.TransformFinalBlock(block, 0, block.Length);
        }

        return crypto.IV;
    }



    /// <summary>
    /// Decodes the payload
    /// </summary>
    /// <param name="payload">Cyphertext of the message payload</param>
    /// <param name="iv">crypto initialization vector</param>
    /// <returns>Message payload as plaintext</returns>
    protected byte[] DecodePayload(byte[] payload, byte[] iv)
    {
        if (!IsSecurityEstablished)
        {
            throw new SecureChannelRequired();
        }
            
        if (payload.Length == 0)
        {
            return Array.Empty<byte>();
        }
        if (payload.Length % 16 != 0)
        {
            throw new Exception($"Unexpected payload length: {payload.Length}");
        }

        using var crypto = SecurityContext.CreateCypher(Context.Enc, false);
        crypto.IV = iv.Select(b => (byte)~b).ToArray();

        using var encryptor = crypto.CreateDecryptor();
        return encryptor.TransformFinalBlock(payload, 0, payload.Length);
    }

    /// <summary>
    /// Encodes the payload
    /// </summary>
    /// <param name="payload">Message payload as plaintext</param>
    /// <param name="iv">Crypto initialization vector</param>
    /// <param name="destination">Destination where cyphertext is to be written</param>
    /// <returns>Cyphertext of the message payload</returns>
    protected void EncodePayload(byte[] payload, byte[] iv, Span<byte> destination)
    {
        if (!IsSecurityEstablished)
        {
            throw new SecureChannelRequired();
        }
        
        if (payload.Length > 0)
        {
            if (payload.Length % 16 != 0)
            {
                throw new Exception($"Unexpected payload length: {payload.Length}");
            }

            using var crypto = SecurityContext.CreateCypher(Context.Enc, false);
            crypto.IV = iv.Select(b => (byte)~b).ToArray();

            using var encryptor = crypto.CreateEncryptor();
            encryptor.TransformFinalBlock(payload, 0, payload.Length).CopyTo(destination);
        }
    }
}