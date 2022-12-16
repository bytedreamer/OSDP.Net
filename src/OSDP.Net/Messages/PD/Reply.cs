using OSDP.Net.Model.ReplyData;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSDP.Net.Messages.PD
{
    public class Reply : Message
    {
        private const int startOfMessageLength = 5;

        IncomingMessage _issuingCommand;
        ReplyData _data;

        public Reply(IncomingMessage issuingCommand, ReplyData data)
        {
            _issuingCommand = issuingCommand;
            _data = data;
        }

        public ReplyType Type => _data.ReplyType;

        public IncomingMessage IssuingCommand => _issuingCommand;

        public byte[] BuildReply(IMessageChannel channel)
        {
            var payload = _data.BuildData(withPadding: channel.IsSecurityEstablished);
            bool isSecurityBlockPresent = channel.IsSecurityEstablished ||
                _data.ReplyType == ReplyType.CrypticData || _data.ReplyType == ReplyType.InitialRMac;
            int headerLength = startOfMessageLength + (isSecurityBlockPresent ? 3 : 0) + sizeof(ReplyType);
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
            curLen += startOfMessageLength;

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

            curLen += channel.PackPayload(payload, buffer.AsSpan(curLen));

            if (channel.IsSecurityEstablished)
            {
                var mac = channel.GenerateMac(buffer.AsSpan(0, curLen), false).Slice(0, MacSize);
                mac.CopyTo(buffer.AsSpan(curLen));
                curLen += mac.Length;
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

        protected override ReadOnlySpan<byte> Data()
        {
            return _data.BuildData();
        }
    }
}
