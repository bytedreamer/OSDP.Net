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

        internal static OutputStatus ParseData(ReadOnlySpan<byte> data)
        {
            return new OutputStatus {OutputStatuses = data.ToArray().Select(Convert.ToBoolean)};
        }

        /// <inheritdoc />
        public override string ToString()
        {
            byte outputNumber = 0;
            var build = new StringBuilder();
            foreach (bool outputStatus in OutputStatuses)
            {
                build.AppendLine($"Output Number {outputNumber++:00}: {outputStatus}");
            }

            return build.ToString();
        }
    }
}