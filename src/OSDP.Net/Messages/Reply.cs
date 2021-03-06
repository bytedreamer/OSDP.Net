using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDP.Net.Messages
{
    internal abstract class Reply : Message
    {
        private const byte AddressMask = 0x7F;
        private const ushort ReplyMessageHeaderSize = 6;
        private const ushort ReplyTypeIndex = 5;
        private const ushort MacSize = 4;

        private readonly Command _issuingCommand;

        protected Reply()
        {
        }

        protected Reply(ReadOnlySpan<byte> data, Guid connectionId, Command issuingCommand, Device device)
        {
            Address = (byte) (data[1] & AddressMask);
            Sequence = (byte) (data[4] & 0x03);
            bool isUsingCrc = Convert.ToBoolean(data[4] & 0x04);
            ushort replyMessageFooterSize = (ushort) (isUsingCrc ? 2 : 1);
            bool isSecureControlBlockPresent = Convert.ToBoolean(data[4] & 0x08);
            byte secureBlockSize = (byte) (isSecureControlBlockPresent ? data[5] : 0);
            SecurityBlockType = (byte) (isSecureControlBlockPresent ? data[6] : 0);
            int messageLength = data.Length - (isUsingCrc ? 6 : 5);
            if (isSecureControlBlockPresent)
            {
                SecureBlockData = data.Slice(ReplyMessageHeaderSize + 2, secureBlockSize - 2).ToArray();
                Mac = data.Slice(messageLength, MacSize).ToArray();
            }
            Type = (ReplyType) data[ReplyTypeIndex + secureBlockSize];

            ExtractReplyData = data.Slice(ReplyMessageHeaderSize + secureBlockSize, data.Length -
                ReplyMessageHeaderSize - secureBlockSize - replyMessageFooterSize -
                (IsSecureMessage ? MacSize : 0)).ToArray();
            if (SecurityBlockType == (byte) OSDP.Net.Messages.SecurityBlockType.ReplyMessageWithDataSecurity)
            {
                ExtractReplyData = DecryptData(device).ToArray();
            }

            IsDataCorrect = isUsingCrc
                ? CalculateCrc(data.Slice(0, data.Length - 2)) ==
                  ConvertBytesToShort(data.Slice(data.Length - 2, 2))
                : CalculateChecksum(data.Slice(0, data.Length - 1).ToArray()) == data[^1];
            MessageForMacGeneration = data.Slice(0, messageLength).ToArray();

            ConnectionId = connectionId;
            _issuingCommand = issuingCommand;
        }

        protected byte SecurityBlockType { get; }
        protected IEnumerable<byte> SecureBlockData { get; }
        private IEnumerable<byte> Mac { get; }
        private bool IsDataCorrect { get; }
        public byte Sequence { get; }
        private bool IsCorrectAddress => _issuingCommand.Address == Address;

        private static IEnumerable<byte> SecureSessionMessages => new[]
        {
            (byte) OSDP.Net.Messages.SecurityBlockType.CommandMessageWithNoDataSecurity,
            (byte) OSDP.Net.Messages.SecurityBlockType.ReplyMessageWithNoDataSecurity,
            (byte) OSDP.Net.Messages.SecurityBlockType.CommandMessageWithDataSecurity,
            (byte) OSDP.Net.Messages.SecurityBlockType.ReplyMessageWithDataSecurity,
        };

        public ReplyType Type { get; }
        public byte[] ExtractReplyData { get; }
        public IEnumerable<byte> MessageForMacGeneration { get; }
        public bool IsSecureMessage => SecureSessionMessages.Contains(SecurityBlockType);

        protected abstract byte ReplyCode { get; }

        public bool IsValidReply => IsCorrectAddress && IsDataCorrect;

        public Guid ConnectionId { get; }

        public static Reply Parse(ReadOnlySpan<byte> data, Guid connectionId, Command issuingCommand, Device device)
        {
            var reply = new UnknownReply(data, connectionId, issuingCommand, device);
            
            return reply;
        }

        public bool SecureCryptogramHasBeenAccepted() => Convert.ToBoolean(SecureBlockData.First());
        public bool MatchIssuingCommand(Command command) => command.Equals(_issuingCommand);
        public bool IsValidMac(ReadOnlySpan<byte> mac) => mac.Slice(0, MacSize).SequenceEqual(Mac.ToArray());

        internal byte[] BuildReply(byte address, Control control)
        {
            var commandBuffer = new List<byte>
            {
                StartOfMessage,
                (byte)(address | 0x80),
                0x0,
                0x0,
                control.ControlByte
            };

            if ( control.HasSecurityControlBlock)
            {
                commandBuffer.AddRange(SecurityControlBlock());
            }

            commandBuffer.Add(ReplyCode);

/*            if (device.IsSecurityEstablished)
            {
                commandBuffer.AddRange(EncryptedData(device));
                
                // include mac and crc in length before generating mac
                AddPacketLength(commandBuffer, (ushort) (4 + (device.MessageControl.UseCrc ? 2 : 1)));

                commandBuffer.AddRange(device.GenerateMac(commandBuffer.ToArray(), true).Take(4));
            }
            else
            {*/
                commandBuffer.AddRange(Data().ToArray());
            //}

            commandBuffer.Add(0x0);
            
            if (control.UseCrc)
            {
                commandBuffer.Add(0x0);
            }

            AddPacketLength(commandBuffer.ToArray());

            if (control.UseCrc)
            {
                AddCrc(commandBuffer.ToArray());
            }
            else
            {
                AddChecksum(commandBuffer.ToArray());
            }
            
            return commandBuffer.ToArray();
        }

        protected abstract IEnumerable<byte> SecurityControlBlock();

        public override string ToString()
        {
            return $"Connection ID: {ConnectionId} Address: {Address} Type: {Type}";
        }

        private IEnumerable<byte> DecryptData(Device device)
        {
            return device.DecryptData(ExtractReplyData.ToArray());
        }
    }
}