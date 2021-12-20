using System.Collections.Generic;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Command data to control the outputs of a PD.
    /// </summary>
    public class OutputControls
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OutputControls"/> class.
        /// </summary>
        /// <param name="controls">The controls.</param>
        public OutputControls(IEnumerable<OutputControl> controls)
        {
            Controls = controls;
        }

        /// <summary>
        /// One or more commands to control the outputs of a PD.
        /// </summary>
        public IEnumerable<OutputControl> Controls { get; }

        internal IEnumerable<byte> BuildData()
        {
            var data = new List<byte>();
            foreach (var outputControl in Controls)
            {
                data.AddRange(outputControl.BuildData());
            }

            return data;
        }
    }
}