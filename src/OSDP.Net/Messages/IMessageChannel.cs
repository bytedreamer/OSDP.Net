using System;

namespace OSDP.Net.Messages
{
    /// <summary>
    /// Represents the channel context within which a message is packed/unpacked
    /// </summary>
    public interface IMessageChannel
    {
        /// <summary>
        /// Indicates whether or not a secure channel has been established on the
        /// current message channel
        /// </summary>
        bool IsSecurityEstablished { get; }

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
        /// Generates a new Message Authentication Code. Note this command IS NOT
        /// idempotent as every time it is called it will calculate a new rolling MAC
        /// value based on the results of the previous call
        /// </summary>
        /// <param name="message">Message to be signed with a MAC</param>
        /// <param name="isCommand">Indicates whether the message is a command or a response</param>
        /// <returns></returns>
        ReadOnlySpan<byte> GenerateMac(ReadOnlySpan<byte> message, bool isCommand);
    }
}
