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

        private readonly Guid _connectionId;
        private readonly Command _issuingCommand;

        protected Reply()
        {
        }

        protected Reply(IReadOnlyList<byte> data, Guid connectionId, Command issuingCommand, Device device)
        {
            Address = (byte) (data[1] & AddressMask);
            Sequence = (byte) (data[4] & 0x03);
            bool isUsingCrc = Convert.ToBoolean(data[4] & 0x04);
            ushort replyMessageFooterSize = (ushort) (isUsingCrc ? 2 : 1);
            bool isSecureControlBlockPresent = Convert.ToBoolean(data[4] & 0x08);
            byte secureBlockSize = (byte) (isSecureControlBlockPresent ? data[5] : 0);
            SecurityBlockType = (byte) (isSecureControlBlockPresent ? data[6] : 0);
            int messageLength = data.Count - (isUsingCrc ? 6 : 5);
            SecureBlockData = data.Skip(ReplyMessageHeaderSize + 2).Take(secureBlockSize - 2);
            Mac = data.Skip(messageLength).Take(MacSize).ToArray();
            Type = (ReplyType) data[ReplyTypeIndex + secureBlockSize];
            
            
            ExtractReplyData = data.Skip(ReplyMessageHeaderSize + secureBlockSize)
                    .Take(data.Count - ReplyMessageHeaderSize - secureBlockSize - replyMessageFooterSize -
                          (IsSecureMessage ? MacSize : 0));
            if (SecurityBlockType == (byte) OSDP.Net.Messages.SecurityBlockType.ReplyMessageWithDataSecurity)
            {
                ExtractReplyData = DecryptData(device);
            }
            
            IsDataCorrect = isUsingCrc
                ? CalculateCrc(data.Take(data.Count - 2).ToArray()) ==
                  ConvertBytesToShort(data.Skip(data.Count - 2).Take(2).ToArray())
                : CalculateChecksum(data.Take(data.Count - 1).ToArray()) == data.Last();
            MessageForMacGeneration = data.Take(messageLength).ToArray();

            _connectionId = connectionId;
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
        public IEnumerable<byte> ExtractReplyData { get; }
        public byte[] MessageForMacGeneration { get; }
        public bool IsSecureMessage => SecureSessionMessages.Contains(SecurityBlockType);

        protected abstract byte ReplyCode { get; }

        public bool IsValidReply => IsCorrectAddress && IsDataCorrect;

        public static Reply Parse(IReadOnlyList<byte> data, Guid connectionId, Command issuingCommand, Device device)
        {
            var reply = new UnknownReply(data, connectionId, issuingCommand, device);

            return reply;
        }

        public bool SecureCryptogramHasBeenAccepted() => Convert.ToBoolean(SecureBlockData.First());
        public bool MatchIssuingCommand(Command command) => command.Equals(_issuingCommand);
        public bool IsValidMac(IEnumerable<byte> mac) => mac.Take(MacSize).SequenceEqual(Mac);

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
                commandBuffer.AddRange(Data());
            //}

            commandBuffer.Add(0x0);
            
            if (control.UseCrc)
            {
                commandBuffer.Add(0x0);
            }

            AddPacketLength(commandBuffer);

            if (control.UseCrc)
            {
                AddCrc(commandBuffer);
            }
            else
            {
                AddChecksum(commandBuffer);
            }
            
            return commandBuffer.ToArray();
        }

        protected abstract IEnumerable<byte> SecurityControlBlock();

        public override string ToString()
        {
            return $"Connection ID: {_connectionId} Address: {Address} Type: {Type}";
        }

        private IEnumerable<byte> DecryptData(Device device)
        {
            return device.DecryptData(ExtractReplyData);
        }
    }
}