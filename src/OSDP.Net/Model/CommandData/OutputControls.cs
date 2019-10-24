using System.Collections.Generic;
using System.Linq;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    public class OutputControls
    {
        public OutputControls(IEnumerable<OutputControl> controls)
        {
            Controls = controls;
        }

        public IEnumerable<OutputControl> Controls { get; }

        public IEnumerable<byte> BuildData()
        {
            var data = new List<byte>();
            foreach (var outputControl in Controls)
            {
                data.AddRange(outputControl.BuildData());
            }

            return data;
        }
    }
    
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
            var timerBytes = Message.ConvertShortToBytes(Timer).ToArray();
            
            return new[] {OutputNumber, (byte) OutputControlCode, timerBytes[0], timerBytes[1]};
        }
    }

    public enum OutputControlCode
    {
        Nop = 0x00,
        PermanentStateOffAbortTimedOperation = 0x01,
        PermanentStateOnAbortTimedOperation = 0x02,
        PermanentStateOffAllowTimedOperation = 0x03,
        PermanentStateOnAllowTimedOperation = 0x04,
        TemporaryStateOnResumePermanentState = 0x05,
        TemporaryStateOffResumePermanentState = 0x06
    }
}