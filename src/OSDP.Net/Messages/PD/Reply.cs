using OSDP.Net.Model.ReplyData;
using System;
using OSDP.Net.Messages.SecureChannel;

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
        /// <param name="secureChannel">Message secure channel context</param>
        /// <returns>Byte representation of the message that is ready to be sent over the
        /// wire to the ACU</returns>
        public byte[] BuildReply(IMessageSecureChannel secureChannel, byte[] prefix=null)
        {
            // TODO: Similar to IncomingMessage, it might make more sense for this code to 
            // eventually end up in a new class called OutgoingMessage

            var payload = _data.BuildData();
            if (secureChannel.IsSecurityEstablished)
            {
                payload = PadTheData(payload, 16, Message.FirstPaddingByte);
            }

            bool isSecurityBlockPresent = secureChannel.IsSecurityEstablished ||
                                          _data.ReplyType == ReplyType.CrypticData ||
                                          _data.ReplyType == ReplyType.InitialRMac;
            int headerLength = StartOfMessageLength + (isSecurityBlockPresent ? 3 : 0) + sizeof(ReplyType);
            int totalLength = (prefix?.Length ?? 0) + headerLength + payload.Length +
                              (_issuingCommand.IsUsingCrc ? 2 : 1) +
                              (secureChannel.IsSecurityEstablished ? MacSize : 0);
            var buffer = new byte[totalLength];
            int currentLength = 0;

            if (prefix != null)
            {
                prefix.CopyTo(buffer, currentLength);
                currentLength += prefix.Length;
            }

            buffer[currentLength] = StartOfMessage;
            buffer[currentLength + 1] = Address;
            buffer[currentLength + 2] = (byte)(totalLength & 0xff);
            buffer[currentLength + 3] = (byte)((totalLength >> 8) & 0xff);
            buffer[currentLength + 4] = (byte)(
                (_issuingCommand.Sequence & 0x03) |
                (_issuingCommand.IsUsingCrc ? 0x04 : 0x00) |
                (isSecurityBlockPresent ? 0x08 : 0x00));
            currentLength += StartOfMessageLength;

            if (isSecurityBlockPresent)
            {
                buffer[currentLength] = 0x03;
                buffer[currentLength + 1] = _data.ReplyType == ReplyType.CrypticData
                    ? (byte)SecurityBlockType.SecureConnectionSequenceStep2
                    : _data.ReplyType == ReplyType.InitialRMac
                        ? (byte)SecurityBlockType.SecureConnectionSequenceStep4
                        : payload.Length == 0
                            ? (byte)SecurityBlockType.ReplyMessageWithNoDataSecurity
                            : (byte)SecurityBlockType.ReplyMessageWithDataSecurity;

                // TODO: How do I determine this properly?? (SCBK vs SCBK-D value)
                // Is this needed only for establishing secure channel? or do we always need to return it
                // with every reply?
                buffer[currentLength + 2] = 0x01;
                currentLength += 3;
            }

            buffer[currentLength] = (byte)_data.ReplyType;
            currentLength++;

            if (secureChannel.IsSecurityEstablished)
            {
                secureChannel.EncodePayload(payload, buffer.AsSpan(currentLength));
                currentLength += payload.Length;
                secureChannel.GenerateMac(buffer.AsSpan(0, currentLength), false)
                    .Slice(0, MacSize)
                    .CopyTo(buffer.AsSpan(currentLength));
                currentLength += MacSize;
            }
            else
            {
                payload.CopyTo(buffer, currentLength);
                currentLength += payload.Length;
            }

            // TODO: decide on CRC vs Checksum based on incoming command and do the same.
            // Is this a valid assumption??
            if (_issuingCommand.IsUsingCrc)
            {
                AddCrc(buffer);
                currentLength += 2;
            }
            else
            {
                AddChecksum(buffer);
                currentLength++;
            }

            if (currentLength != buffer.Length)
            {
                throw new Exception(
                    $"Invalid processing of reply data, expected length {currentLength}, actual length {buffer.Length}");
            }

            return buffer;
        }

        /// <inheritdoc/>
        protected override ReadOnlySpan<byte> Data()
        {
            return _data.BuildData();
        }
    }
}
