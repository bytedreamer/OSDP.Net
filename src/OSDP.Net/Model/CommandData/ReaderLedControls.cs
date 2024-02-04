using System;
using System.Collections.Generic;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Command data to control the color of LEDs.
    /// </summary>
    public class ReaderLedControls : CommandData
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

        /// <inheritdoc />
        public override CommandType CommandType => CommandType.LEDControl;
        
        /// <inheritdoc />
        public override byte Type => (byte)CommandType.LEDControl;

        /// <inheritdoc />
        public override byte[] BuildData()
        {
            var data = new List<byte>();
            foreach (var readerLedControl in Controls)
            {
                data.AddRange(readerLedControl.BuildData());
            }

            return data.ToArray();
        }
        
        /// <summary>Parses the message payload bytes</summary>
        /// <param name="payloadData">Message payload as bytes</param>
        /// <returns>An instance of ReaderLedControls representing the message payload</returns>
        public static ReaderLedControls ParseData(ReadOnlySpan<byte> payloadData)
        {
            return new ReaderLedControls(SplitData(14, data => ReaderLedControl.ParseData(data), payloadData));
        }
    }
}