using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDP.Net.Messages
{
    public class Reply : Message
    {
        private const byte AddressMask = 0x7F;
        private const ushort ReplyMessageHeaderSize = 6;
        private const ushort ReplyTypeIndex = 5;
        private const ushort MacSize = 4;

        private readonly Guid _connectionId;
        private readonly IReadOnlyList<byte> _data;
        private readonly Command _issuingCommand;

        public Reply(IReadOnlyList<byte> data, Command issuingCommand, Guid connectionId)
        {
            _data = data;
            _issuingCommand = issuingCommand;
            _connectionId = connectionId;
        }

        private byte Address => (byte) (_data[1] & AddressMask);

        private ushort ReplyMessageFooterSize => (ushort) (IsUsingCrc ? 2 : 1);

        private bool IsSecureControlBlockPresent => Convert.ToBoolean(_data[4] & 0x08);

        private bool IsUsingCrc => Convert.ToBoolean(_data[4] & 0x04);

        private byte SecureBlockSize => (byte) (IsSecureControlBlockPresent ? _data[5] : 0);

        private byte SecurityBlockType => (byte) (IsSecureControlBlockPresent ? _data[6] : 0);

        private static IEnumerable<byte> SecureSessionMessages => new []
        {
            (byte) OSDP.Net.Messages.SecurityBlockType.CommandMessageWithNoDataSecurity,
            (byte) OSDP.Net.Messages.SecurityBlockType.ReplyMessageWithNoDataSecurity,
            (byte) OSDP.Net.Messages.SecurityBlockType.CommandMessageWithDataSecurity,
            (byte) OSDP.Net.Messages.SecurityBlockType.ReplyMessageWithDataSecurity,
        };

        private int MessageLength => _data.Count - (IsUsingCrc ? 6 : 5);

        private IEnumerable<byte> SecureBlockData => _data.Skip(ReplyMessageHeaderSize + 2).Take(SecureBlockSize - 2);

        private IEnumerable<byte> Mac => _data.Skip(MessageLength).Take(MacSize).ToArray();

        public ReplyType Type => (ReplyType) _data[ReplyTypeIndex + SecureBlockSize];

        public IEnumerable<byte> ExtractReplyData =>
            _data.Skip(ReplyMessageHeaderSize).Skip(SecureBlockSize)
                .Take(_data.Count - ReplyMessageHeaderSize - SecureBlockSize - ReplyMessageFooterSize);

        public bool IsSecureMessage => SecureSessionMessages.Contains(SecurityBlockType);

        private bool IsCorrectAddress() => _issuingCommand.Address == Address;

        private bool IsDataCorrect() =>
            IsUsingCrc
                ? CalculateCrc(_data.Take(_data.Count - 2).ToArray()) ==
                  ConvertBytesToShort(_data.Skip(_data.Count - 2).Take(2).ToArray())
                : CalculateChecksum(_data.Take(_data.Count - 1).ToArray()) == _data.Last();

        public byte[] MessageForMacGeneration() => _data.Take(MessageLength).ToArray();

        public bool IsValidReply() => IsCorrectAddress() && IsDataCorrect();

        public bool SecureCryptogramHasBeenAccepted() => Convert.ToBoolean(SecureBlockData.First());

        public bool MatchIssuingCommand(Command command) => command.Equals(_issuingCommand);

        public bool IsValidMac(IEnumerable<byte> mac)
        {
            return mac.Take(MacSize).SequenceEqual(Mac);
        }

        public override string ToString()
        {
            return $"Connection ID: {_connectionId} Address: {Address} Type: {Type}";
        }
    }
}