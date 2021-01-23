using System.Collections.Generic;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Command data to control the color of LEDs.
    /// </summary>
    public class ReaderLedControls
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReaderLedControls"/> class.
        /// </summary>
        /// <param name="controls">The controls.</param>
        public ReaderLedControls(IEnumerable<ReaderLedControl> controls)
        {
            Controls = controls;
        }

        /// <summary>
        /// One or more commands to control the LEDs of a PD.
        /// </summary>
        public IEnumerable<ReaderLedControl> Controls { get; }

        public IEnumerable<byte> BuildData()
        {
            var data = new List<byte>();
            foreach (var readerLedControl in Controls)
            {
                data.AddRange(readerLedControl.BuildData());
            }

            return data;
        }
    }
}