using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// The reader status report sent as a reply.
    /// </summary>
    public class ReaderStatus
    {
        /// <summary>
        /// Possible states that the reader is currently reporting.
        /// </summary>
        public enum ReaderTamperStatus
        {
            /// <summary>Reader is in a normal state.</summary>
            Normal = 0x00,

            /// <summary>Reader is in a not connected state.</summary>
            NotConnected = 0x01,

            /// <summary>Reader is in a tamper state.</summary>
            Tamper = 0x02
        }

        private ReaderStatus()
        {
        }

        /// <summary>
        /// Gets the all the PDs reader statuses as an array ordered by reader number.
        /// </summary>
        public IEnumerable<ReaderTamperStatus> ReaderTamperStatuses { get; private set; }

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
                build.AppendLine($"Reader Number {readerNumber++:00}: {Message.SplitCamelCase(readerStatuses.ToString())}");
            }

            return build.ToString();
        }
    }
}