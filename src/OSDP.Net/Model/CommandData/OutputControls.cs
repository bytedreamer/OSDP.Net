using System;
using System.Collections.Generic;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Command data to control the outputs of a PD.
    /// </summary>
    public class OutputControls : CommandData
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
        
        /// <inheritdoc />
        public override CommandType CommandType => CommandType.OutputControl;
        
        /// <inheritdoc />
        public override byte Type => (byte)CommandType.OutputControl;

        /// <inheritdoc />
        public override byte[] BuildData()
        {
            var data = new List<byte>();
            foreach (var outputControl in Controls)
            {
                data.AddRange(outputControl.BuildData());
            }

            return data.ToArray();
        }

        /// <summary>Parses the message payload bytes</summary>
        /// <param name="payloadData">Message payload as bytes</param>
        /// <returns>An instance of OutputControls representing the message payload</returns>
        public static OutputControls ParseData(ReadOnlySpan<byte> payloadData)
        {
            return new OutputControls(SplitData(4, data => OutputControl.ParseData(data), payloadData));
        }
    }
}