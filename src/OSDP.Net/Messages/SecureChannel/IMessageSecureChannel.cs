using System;

namespace OSDP.Net.Messages.SecureChannel;

/// <summary>
/// Represents the secure channel context within which a message is packed/unpacked
/// </summary>
public interface IMessageSecureChannel
{
    /// <summary>
    /// Indicates whether or not a secure channel has been established on the
    /// current message channel
    /// </summary>
    bool IsSecurityEstablished { get; }

    public bool IsInitialized { get; }
    
    public byte[] ServerRandomNumber { get; }
    
    public byte[] ServerCryptogram { get; }

    /// <summary>
    /// Encrypts the payload using the secure channel context. This function can
    /// only be called once IsSecurityEstablished is true.
    /// </summary>
    /// <param name="payload">Payload to be encrypted. NOTE: This data MUST already
    /// be padded in according to OSDP specification: a) there must be at least one
    /// byte of padding; b) payloaded is padded with 0x00, except for the very first
    /// byte of padding which must be 0x80; c) total length must be divisible by
    /// AES block size of 16 bytes.</param>
    /// <param name="destination">Identifies where encoded data is to written to</param>
    void EncodePayload(byte[] payload, Span<byte> destination);

    /// <summary>
    /// Decodes the payload using the secure channel context. This function can
    /// only be called once IsSecurityEstablished is true
    /// </summary>
    /// <param name="payload">Payload to be decrypted</param>
    /// <returns>
    /// Plaintext message payload. Similarly to how original payload must have been
    /// padded prior to being encrypted, the plaintext byte buffer returned here will
    /// also be padded and therefore its length will always be divisible by 16. The 
    /// padding at the end of the buffer will always consist of zero or more 0x00
    /// bytes preceeded by 0x80 byte.
    /// </returns>
    byte[] DecodePayload(byte[] payload);

    /// <summary>
    /// Generates a new Message Authentication Code. Note this command IS NOT
    /// idempotent as every time it is called it will calculate a new rolling MAC
    /// value based on the results of the previous call
    /// </summary>
    /// <param name="message">Message to be signed with a MAC</param>
    /// <param name="isIncoming">If true, indicates that the message was received
    /// from the wire. If false, indicates that the message is being sent out 
    /// on the wire</param>
    /// <returns>16-byte MAC value of the message</returns>
    ReadOnlySpan<byte> GenerateMac(ReadOnlySpan<byte> message, bool isIncoming);

    void InitializeACU(byte[] clientRandomNumber, byte[] clientCryptogram);
    
    void ResetSecureChannelSession();
    
    void Establish(byte[] rmac);
    
    ReadOnlySpan<byte> PadTheData(ReadOnlySpan<byte> payload);
}