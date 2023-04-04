using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// A reader status report reply.
    /// </summary>
    public class ReaderStatus
    {
        private ReaderStatus()
        {
        }

        /// <summary>
        /// Gets the all the PDs reader statuses as an array ordered by reader number.
        /// </summary>
        public IEnumerable<ReaderTamperStatus> ReaderTamperStatuses { get; private set; }

        /// <summary>Parses the message payload bytes</summary>
        /// <param name="data">Message payload as bytes</param>
        /// <returns>An instance of ReaderStatus representing the message payload</returns>
        internal static ReaderStatus ParseData(ReadOnlySpan<byte> data)
        {
            return new ReaderStatus
                {ReaderTamperStatuses = data.ToArray().Select(status => (ReaderTamperStatus) status)};
        }

        /// <inheritdoc />
        public override string ToString()
        {
            byte readerNumber = 0;
            var build = new StringBuilder();
            foreach (var readerStatuses in ReaderTamperStatuses)
            {
                build.AppendLine($"Reader Number {readerNumber++:00}: {Helpers.SplitCamelCase(readerStatuses.ToString())}");
            }

            return build.ToString();
        }
    }
}