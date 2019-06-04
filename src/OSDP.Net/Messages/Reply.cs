using System.Collections.Generic;
using System.Linq;

namespace OSDP.Net.Messages
{
    public class Reply : Message
    {
        private readonly IReadOnlyCollection<byte> _data;

        public Reply(IReadOnlyCollection<byte> data)
        {
            _data = data;
        }

        public bool IsValidReply(Command command)
        {
            return command.Control.UseCrc
                ? CalculateCrc(_data.Take(_data.Count - 2).ToArray()) ==
                  ConvertBytesToShort(_data.Skip(_data.Count - 2).Take(2).ToArray())
                : CalculateChecksum(_data.Take(_data.Count - 1).ToArray()) == _data.Last();
        }
    }
}