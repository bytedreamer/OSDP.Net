using OSDP.Net.Model.ReplyData;
using System;
using System.Diagnostics;

namespace OSDP.Net.Messages.PD
{
    /// <summary>
    /// Represents an outgoing reply (PD -> ACU) message
    /// </summary>
    public class Reply : Message
    {
        private const int StartOfMessageLength = 5;
        private readonly IncomingMessage _issuingCommand;
        private readonly ReplyData _data;

        /// <summary>
        /// Creates a new instance of the Reply
        /// </summary>
        /// <param name="issuingCommand">Incoming command message that this is in reply to</param>
        /// <param name="data">the payload portion of the reply message</param>
        public Reply(IncomingMessage issuingCommand, ReplyData data)
        {
            _issuingCommand = issuingCommand;
            _data = data;
        }

        /// <summary>
        /// Reply code of the message
        /// </summary>
        public ReplyType Type => _data.ReplyType;

        /// <summary>
        /// Incoming command that this instance is replying to
        /// </summary>
        public IncomingMessage IssuingCommand => _issuingCommand;

        /// <summary>
        /// Packs message information into a byte array
        /// </summary>
        /// <param name="channel">Message channel context</param>
        /// <returns>Byte representation of the message that is ready to be sent over the
        /// wire to the ACU</returns>
        public byte[] BuildReply(IMessageChannel channel)
        {
            // TODO: Similar to IncomingMessage, it might make more sense for this code to 
            // eventually end up in a new class called OutgoingMessage

            var payload = _data.BuildData(withPadding: channel.IsSecurityEstablished);
            bool isSecurityBlockPresent = channel.IsSecurityEstablished ||
                _data.ReplyType == ReplyType.CrypticData || _data.ReplyType == ReplyType.InitialRMac;
            int headerLength = StartOfMessageLength + (isSecurityBlockPresent ? 3 : 0) + sizeof(ReplyType);
            int totalLength = headerLength + payload.Length + 
                (_issuingCommand.IsUsingCrc ? 2 : 1) + (channel.IsSecurityEstablished ? MacSize : 0);
            var buffer = new byte[totalLength];
            int curLen = 0;

            buffer[0] = StartOfMessage;
            buffer[1] = Address;
            buffer[2] = (byte)(totalLength & 0xff);
            buffer[3] = (byte)((totalLength >> 8) & 0xff);
            buffer[4] = (byte)(
                (_issuingCommand.Sequence & 0x03) |
                (_issuingCommand.IsUsingCrc ? 0x04 : 0x00) |
                (isSecurityBlockPresent ? 0x08 : 0x00));
            curLen += StartOfMessageLength;

            if (isSecurityBlockPresent)
            {
                buffer[curLen] = 0x03;
                buffer[curLen + 1] = _data.ReplyType == ReplyType.CrypticData
                    ? (byte)SecurityBlockType.SecureConnectionSequenceStep2
                    : _data.ReplyType == ReplyType.InitialRMac
                    ? (byte)SecurityBlockType.SecureConnectionSequenceStep4
                    : payload.Length == 0
                    ? (byte)SecurityBlockType.ReplyMessageWithNoDataSecurity
                    : (byte)SecurityBlockType.ReplyMessageWithDataSecurity;

                // TODO: How do I determine this properly?? (SCBK vs SCBK-D value)
                buffer[curLen + 2] = 0x00;
                curLen += 3;
            }

            buffer[curLen] = (byte)_data.ReplyType;
            curLen++;

            if (channel.IsSecurityEstablished)
            {
                channel.EncodePayload(payload, buffer.AsSpan(curLen));
                curLen += payload.Length;
                channel.GenerateMac(buffer.AsSpan(0, curLen), false)
                    .Slice(0, MacSize)
                    .CopyTo(buffer.AsSpan(curLen));
                curLen += MacSize;
            }
            else
            {
                payload.CopyTo(buffer, curLen);
                curLen += payload.Length;
            }

            // TODO: decide on CRC vs Checksum based on incoming command and do the same.
            // Is this a valid assumption??
            if (_issuingCommand.IsUsingCrc)
            {
                AddCrc(buffer);
                curLen += 2;
            }
            else
            {
                AddChecksum(buffer);
                curLen++;
            }

            Debug.Assert(curLen == buffer.Length);

            return buffer;
        }

        /// <inheritdoc/>
        protected override ReadOnlySpan<byte> Data()
        {
            return _data.BuildData();
        }
    }
}
