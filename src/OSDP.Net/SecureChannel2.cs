using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using OSDP.Net.Messages;
using OSDP.Net.Messages.PD;
using OSDP.Net.Model.ReplyData;

namespace OSDP.Net
{
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
    public abstract class MessageChannel : IMessageChannel
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
        public MessageChannel(SecurityContext context = null, ILoggerFactory loggerFactory = null)
        {
            Context = context ?? new();
            Logger = loggerFactory?.CreateLogger(GetType());
        }

        /// <summary>
        /// Security state used by the channel
        /// </summary>
        protected SecurityContext Context { get; private set; }

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
        protected byte[] GenerateCommandMac(ReadOnlySpan<byte> message) => (Context.CMac = GenerateMac(message, Context.RMac));

        /// <summary>
        /// Generates a MAC for a reply message
        /// </summary>
        /// <param name="message">Message bytes to generate code from</param>
        /// <returns>Newly generated MAC</returns>
        /// <exception cref="SecureChannelRequired">Thrown if secure channel has not been established</exception>
        protected byte[] GenerateReplyMac(ReadOnlySpan<byte> message) => (Context.RMac = GenerateMac(message, Context.CMac));

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
            else if (payload.Length == 0)
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
            else if (payload.Length > 0)
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

    /// <summary>
    /// Message channel which represents the Periphery Device (PD) side of the OSDP 
    /// communications (i.e. OSDP commands are received and replies are sent out)
    /// </summary>
    public class PdMessageChannel : MessageChannel
    {
        private byte[] _expectedServerCryptogram;

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
        public PdMessageChannel(SecurityContext context = null, ILoggerFactory loggerFactory = null) 
            : base(context, loggerFactory) {}

        /// <inheritdoc />
        public override void EncodePayload(byte[] payload, Span<byte> destination) =>
            EncodePayload(payload, Context.CMac, destination);

        /// <inheritdoc />
        public override byte[] DecodePayload(byte[] payload) => DecodePayload(payload, Context.RMac);

        /// <inheritdoc />
        public override ReadOnlySpan<byte> GenerateMac(ReadOnlySpan<byte> message, bool isIncoming) =>
            isIncoming ? GenerateCommandMac(message) : GenerateReplyMac(message);

        /// <summary>
        /// Default handler for the SessionChallenge message received on the channel
        /// </summary>
        /// <param name="command">Incoming command of type SessionChallenge</param>
        /// <returns>A message representing a reply to the SessionChallenge</returns>
        protected Reply HandleSessionChallenge(IncomingMessage command)
        {
            // TODO: this should be some kind of unique identifier, but a) not sure how to generate it and b) seems
            // the other side presently simply ignores these bytes. So for time being simply leaving this uninitialized
            byte[] cUID = new byte[8];

            byte[] rndA = command.Payload;
            byte[] rndB = new byte[8];

            // TODO: Although somewhere we do need to store the default key, this must not be 
            // just permanently hardcoded for all sessions. For now this code is only used in the
            // simulator so this is okay, but it is definitely a candidate for next set of enhancements
#pragma warning disable IDE0230 // Use UTF-8 string literal
            byte[] secureChannelKey = new byte[] {
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3a, 0x3b, 0x3c, 0x3d, 0x3e, 0x3f };
#pragma warning restore IDE0230 // Use UTF-8 string literal

            // TODO: we should validate payload and SCB type

            // It is possible that ACU may decide to re-challenge us after a channel was already set up.
            // In that case, let's make sure we clear this flag to indicate that we do NOT in fact have security
            // established
            Context.IsSecurityEstablished = false;

            // generate RND.B
            new Random().NextBytes(rndB);

            // generate a set of sessioon keys: S-ENC, S-MAC1, S-MAC2 using command.Payload (which is RND.A)
            var crypto = SecurityContext.CreateCypher(secureChannelKey, true);

            Context.Enc = SecurityContext.GenerateKey(crypto, new byte[] { 0x01, 0x82, rndA[0], rndA[1], rndA[2], rndA[3], rndA[4], rndA[5] });
            Context.SMac1 = SecurityContext.GenerateKey(crypto, new byte[] { 0x01, 0x01, rndA[0], rndA[1], rndA[2], rndA[3], rndA[4], rndA[5] });
            Context.SMac2 = SecurityContext.GenerateKey(crypto, new byte[] { 0x01, 0x02, rndA[0], rndA[1], rndA[2], rndA[3], rndA[4], rndA[5] });

            // generate client crytpogram
            crypto.Key = Context.Enc;
            var clientCryptogram = SecurityContext.GenerateKey(crypto, rndA, rndB);
            _expectedServerCryptogram = SecurityContext.GenerateKey(crypto, rndB, rndA);

            Logger?.LogInformation($"Rnd.A: {{rndA}}{Environment.NewLine}" +
                $"Rnd.B: {{rndB}}{Environment.NewLine}" +
                $"Enc: {{enc}}{Environment.NewLine}" +
                $"ClientCrypto: {{ccrypto}}{Environment.NewLine}",
                BitConverter.ToString(rndA),
                BitConverter.ToString(rndB),
                BitConverter.ToString(Context.Enc),
                BitConverter.ToString(clientCryptogram));

            // reply with osdp_CCRYPT, returning PD's Id (cUID), its random number and the client cryptogram
            return new Reply(command, new ChallengeResponse(cUID, rndB, clientCryptogram));
        }

        /// <summary>
        /// Default handler to the SCrypt command received on the channel
        /// </summary>
        /// <param name="command">An incoming message representing SCrypt command</param>
        /// <returns>Reply to the SCrypt command</returns>
        protected Reply HandleSCrypt(IncomingMessage command)
        {
            var serverCryptogram = command.Payload;

            if (command.SecurityBlockType != (byte)SecurityBlockType.SecureConnectionSequenceStep3)
            {
                Logger.LogWarning("Received unexpected security block type: {sbt}", command.SecurityBlockType);
            }
            else if (!serverCryptogram.SequenceEqual(_expectedServerCryptogram))
            {
                Logger.LogWarning("Received unexpected server cryptogram!");
            }
            else if (IsSecurityEstablished)
            {
                Logger.LogWarning("Secure channel already established. Why did we get another SCrypt??");
            }
            else
            {
                var crypto = SecurityContext.CreateCypher(Context.SMac1, true);
                Context.RMac = SecurityContext.GenerateKey(crypto, serverCryptogram);
                crypto.Key = Context.SMac2;
                Context.RMac = SecurityContext.GenerateKey(crypto, Context.RMac);

                return new Reply(command, new InitialRMac(Context.RMac));
            }

            return new Reply(command, new Nak(ErrorCode.DoesNotSupportSecurityBlock));
        }
    }

    /// <summary>
    /// Message channel which represents the Access Control Unit (ACU) side of the OSDP 
    /// communications (i.e. OSDP commands are sent out and replies are received)
    /// </summary>
    public class ACUMessageChannel : MessageChannel
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
        public ACUMessageChannel(SecurityContext context = null, ILoggerFactory loggerFactory = null)
            : base(context, loggerFactory) {}

        /// <inheritdoc />
        public override void EncodePayload(byte[] payload, Span<byte> destination) =>
            EncodePayload(payload, Context.RMac, destination);

        /// <inheritdoc />
        public override byte[] DecodePayload(byte[] payload) => DecodePayload(payload, Context.CMac);

        /// <inheritdoc />
        public override ReadOnlySpan<byte> GenerateMac(ReadOnlySpan<byte> message, bool isIncoming) =>
            isIncoming ? GenerateReplyMac(message) : GenerateCommandMac(message);
    }

    /// <summary>
    /// Security context used within SecureChannel2
    /// 
    /// This state data is placed into its own class to facilitate use cases where multiple channels
    /// (i.e. one for incoming packets; one for outgoing) have to share the same security state.
    /// </summary>
    public class SecurityContext
    {
        /// <summary>
        /// A flag indicating whether or not channel security has been established
        /// </summary>
        public bool IsSecurityEstablished { get; set; }

        /// <summary>
        /// Symmertric message encryption key established by the secure channel handshake
        /// </summary>
        public byte[] Enc { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// S-MAC1 value
        /// </summary>
        public byte[] SMac1 { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// S-MAC2 value
        /// </summary>
        public byte[] SMac2 { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// R-MAC value
        /// </summary>
        public byte[] RMac { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// C-MAC value
        /// </summary>
        public byte[] CMac { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Creates a new instance of AES cypher
        /// </summary>
        /// <param name="key">Encryption key to be used</param>
        /// <param name="isForSessionSetup">We use the cypher in two major use cases: 
        /// session setup and message data encryption. Depending on the case, it has 
        /// to be initialized slightly differently so this flag indicates which case 
        /// is currently needed.</param>
        /// <returns>Cypher instance</returns>
        public static Aes CreateCypher(byte[] key, bool isForSessionSetup)
        {
            var crypto = Aes.Create();
            if (crypto == null)
            {
                throw new Exception("Unable to create key algorithm");
            }

            if (!isForSessionSetup)
            {
                crypto.Mode = CipherMode.CBC;
                crypto.Padding = PaddingMode.None;
            }
            else
            {
                crypto.Mode = CipherMode.ECB;
                crypto.Padding = PaddingMode.Zeros;
            }
            crypto.KeySize = 128;
            crypto.BlockSize = 128;
            crypto.Key = key;

            return crypto;
        }

        /// <summary>
        /// Slightly specialized version of simple AES encryption that is 
        /// intended specifically for generating keys used in OSDP secure channel
        /// comms. 
        /// </summary>
        /// <param name="aes">AES crypto instance</param>
        /// <param name="input">Set of bytes to be used as input to generate the 
        /// resulting key. For convenience the caller might pass in more than one
        /// byte array, but the total sum of all bytes MUST be less than or equal
        /// to 16</param>
        /// <returns></returns>
        public static byte[] GenerateKey(Aes aes, params byte[][] input)
        {
            var buffer = new byte[16];
            int currentSize = 0;
            foreach (byte[] x in input)
            {
                x.CopyTo(buffer, currentSize);
                currentSize += x.Length;
            }
            using var encryptor = aes.CreateEncryptor();

            return encryptor.TransformFinalBlock(buffer, 0, buffer.Length);
        }
    }
}
