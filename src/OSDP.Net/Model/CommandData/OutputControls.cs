using System.Collections.Generic;

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