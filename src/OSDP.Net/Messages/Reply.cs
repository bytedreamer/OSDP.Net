using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDP.Net.Messages
{
    public class Reply : Message
    {
        private const byte AddressMask = 0x7F;

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

        public ReplyType Type => (ReplyType) _data[5];

        public IEnumerable<byte> ExtractReplyData =>
            _data.Skip(6).Take(_data.Count - 6 - (_issuingCommand.Control.UseCrc ? 2 : 1));

        public bool IsValidReply()
        {
            return IsCorrectAddress() && IsDataCorrect();
        }

        public override string ToString()
        {
            return $"Connection ID: {_connectionId} Address: {Address} Type: {Type}";
        }

        private bool IsCorrectAddress()
        {
            return _issuingCommand.Address == Address;
        }

        private bool IsDataCorrect()
        {
            return _issuingCommand.Control.UseCrc
                ? CalculateCrc(_data.Take(_data.Count - 2).ToArray()) ==
                  ConvertBytesToShort(_data.Skip(_data.Count - 2).Take(2).ToArray())
                : CalculateChecksum(_data.Take(_data.Count - 1).ToArray()) == _data.Last();
        }
    }
}