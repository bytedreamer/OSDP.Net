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

        private ushort SecureBlockSize => (ushort) (IsSecureControlBlockPresent ? _data[5] : 0);

        private IEnumerable<byte> SecureBlockData => _data.Skip(ReplyMessageHeaderSize + 2).Take(SecureBlockSize - 2);

        public ReplyType Type => (ReplyType) _data[ReplyTypeIndex + SecureBlockSize];

        public IEnumerable<byte> ExtractReplyData =>
            _data.Skip(ReplyMessageHeaderSize).Skip(SecureBlockSize)
                .Take(_data.Count - ReplyMessageHeaderSize - SecureBlockSize - ReplyMessageFooterSize);

        public bool IsValidReply()
        {
            return IsCorrectAddress() && IsDataCorrect();
        }

        public bool SecureCryptogramHasBeenAccepted()
        {
            return Convert.ToBoolean(SecureBlockData.First());
        }

        public override string ToString()
        {
            return $"Connection ID: {_connectionId} Address: {Address} Type: {Type}";
        }

        public bool MatchIssuingCommand(Command command)
        {
            return command.Equals(_issuingCommand);
        }

        private bool IsCorrectAddress()
        {
            return _issuingCommand.Address == Address;
        }

        private bool IsDataCorrect()
        {
            return IsUsingCrc
                ? CalculateCrc(_data.Take(_data.Count - 2).ToArray()) ==
                  ConvertBytesToShort(_data.Skip(_data.Count - 2).Take(2).ToArray())
                : CalculateChecksum(_data.Take(_data.Count - 1).ToArray()) == _data.Last();
        }
    }
}