using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// A output status report reply.
    /// </summary>
    public class OutputStatus
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="OutputStatus"/> class from being created.
        /// </summary>
        private OutputStatus()
        {
        }

        /// <summary>
        /// Gets the all the PDs output statuses as an array ordered by output number.
        /// </summary>
        public IEnumerable<bool> OutputStatuses { get; private set; }

        /// <summary>Parses the message payload bytes</summary>
        /// <param name="data">Message payload as bytes</param>
        /// <returns>An instance of OutputStatus representing the message payload</returns>
        public static OutputStatus ParseData(ReadOnlySpan<byte> data)
        {
            return new OutputStatus {OutputStatuses = data.ToArray().Select(Convert.ToBoolean)};
        }

        /// <inheritdoc/>
        public override string ToString() => ToString(0);

        /// <summary>
        /// Returns a string representation of the current object
        /// </summary>
        /// <param name="indent">Number of ' ' chars to add to beginning of every line</param>
        /// <returns>String representation of the current object</returns>
        public string ToString(int indent)
        {
            var padding = new string(' ', indent);
            byte outputNumber = 0;
            var build = new StringBuilder();
            foreach (bool outputStatus in OutputStatuses)
            {
                build.AppendLine($"{padding}Output Number {outputNumber++:00}: {outputStatus}");
            }
            return build.ToString();
        }
    }
}