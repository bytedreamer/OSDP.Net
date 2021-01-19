using System.Collections.Generic;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    public class OutputControl
    {
        public OutputControl(byte outputNumber, OutputControlCode outputControlCode, ushort timer)
        {
            OutputNumber = outputNumber;
            OutputControlCode = outputControlCode;
            Timer = timer;
        }

        public byte OutputNumber { get; }
        public OutputControlCode OutputControlCode { get; }
        public ushort Timer { get; }

        public IEnumerable<byte> BuildData()
        {
            var timerBytes = Message.ConvertShortToBytes(Timer);
            
            return new[] {OutputNumber, (byte) OutputControlCode, timerBytes[0], timerBytes[1]};
        }
    }
}