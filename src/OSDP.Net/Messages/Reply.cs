using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDP.Net.Messages
{
    public class Reply : Message
    {
        private readonly Guid _connectionId;
        private readonly IReadOnlyList<byte> _data;

        public Reply(IReadOnlyList<byte> data, Guid connectionId)
        {
            _data = data;
            _connectionId = connectionId;
        }

        private byte Address => (byte) (_data[1] & 0x7F);

        public bool IsValidReply(Command command)
        {
            return IsCorrectAddress(command) && IsDataCorrect(command);
        }

        public override string ToString()
        {
            return $"Connection ID: {_connectionId} Address: {Address}";
        }

        private bool IsCorrectAddress(Command command)
        {
            return command.Address == Address;
        }

        private bool IsDataCorrect(Command command)
        {
            return command.Control.UseCrc
                ? CalculateCrc(_data.Take(_data.Count - 2).ToArray()) ==
                  ConvertBytesToShort(_data.Skip(_data.Count - 2).Take(2).ToArray())
                : CalculateChecksum(_data.Take(_data.Count - 1).ToArray()) == _data.Last();
        }
    }
}