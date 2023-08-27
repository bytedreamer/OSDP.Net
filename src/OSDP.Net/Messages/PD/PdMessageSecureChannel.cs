using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model.ReplyData;

namespace OSDP.Net.Messages.PD
{
    /// <summary>
    /// Message channel which represents the Periphery Device (PD) side of the OSDP 
    /// communications (i.e. OSDP commands are received and replies are sent out)
    /// </summary>
    internal class PdMessageSecureChannel : MessageSecureChannel
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

            // TODO: we should validate payload and SCB type

            // It is possible that ACU may decide to re-challenge us after a channel was already set up.
            // In that case, let's make sure we clear this flag to indicate that we do NOT in fact have security
            // established
            Context.IsSecurityEstablished = false;

            // generate RND.B
            new Random().NextBytes(rndB);

            // generate a set of session keys: S-ENC, S-MAC1, S-MAC2 using command.Payload (which is RND.A)
            var crypto = SecurityContext.CreateCypher(SecurityContext.DefaultKey, true);

            Context.Enc = SecurityContext.GenerateKey(crypto, new byte[] { 0x01, 0x82, rndA[0], rndA[1], rndA[2], rndA[3], rndA[4], rndA[5] });
            Context.SMac1 = SecurityContext.GenerateKey(crypto, new byte[] { 0x01, 0x01, rndA[0], rndA[1], rndA[2], rndA[3], rndA[4], rndA[5] });
            Context.SMac2 = SecurityContext.GenerateKey(crypto, new byte[] { 0x01, 0x02, rndA[0], rndA[1], rndA[2], rndA[3], rndA[4], rndA[5] });

            // generate client cryptogram
            crypto.Key = Context.Enc;
            var clientCryptogram = SecurityContext.GenerateKey(crypto, rndA, rndB);
            _expectedServerCryptogram = SecurityContext.GenerateKey(crypto, rndB, rndA);

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
                Logger?.LogWarning("Received unexpected security block type: {SecurityBlockType}",
                    command.SecurityBlockType);
            }
            else if (!serverCryptogram.SequenceEqual(_expectedServerCryptogram))
            {
                Logger?.LogWarning("Received unexpected server cryptogram!");
            }
            else if (IsSecurityEstablished)
            {
                Logger?.LogWarning("Secure channel already established. Why did we get another SCrypt??");
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
}
