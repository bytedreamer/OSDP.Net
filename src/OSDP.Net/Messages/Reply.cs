using System.Collections.Generic;
using System.Linq;

namespace OSDP.Net.Messages
{
    public class Reply : Message
    {
        private readonly IReadOnlyList<byte> _data;

        public Reply(IReadOnlyList<byte> data)
        {
            _data = data;
        }

        public bool IsValidReply(Command command)
        {
            return IsCorrectAddress(command) && IsDataCorrect(command);
        }

        private bool IsCorrectAddress(Command command)
        {
            return command.Address == (_data[1] & 0x7F);
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