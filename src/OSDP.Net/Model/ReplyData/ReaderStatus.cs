using System;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// A reader status report reply.
    /// </summary>
    public class ReaderStatus : PayloadData
    {
        /// <summary>
        /// Creates a new instance of ReaderStatus class
        /// </summary>
        /// <param name="statuses"></param>
        public ReaderStatus(ReaderTamperStatus[] statuses)
        {
            ReaderTamperStatuses = statuses;
        }

        /// <inheritdoc />
        public override byte Code => (byte)ReplyType.ReaderStatusReport;

        /// <summary>
        /// Gets the all the PDs reader statuses as an array ordered by reader number.
        /// </summary>
        public ReaderTamperStatus[] ReaderTamperStatuses { get; }

        /// <inheritdoc />
        public override ReadOnlySpan<byte> SecurityControlBlock() => SecurityBlock.ReplyMessageWithDataSecurity;

        /// <summary>Parses the message payload bytes</summary>
        /// <param name="data">Message payload as bytes</param>
        /// <returns>An instance of ReaderStatus representing the message payload</returns>
        internal static ReaderStatus ParseData(ReadOnlySpan<byte> data)
        {
            return new ReaderStatus(data.ToArray().Select(status => (ReaderTamperStatus) status).ToArray());
        }

        /// <inheritdoc />
        public override byte[] BuildData() => ReaderTamperStatuses.Select(x => (byte)x).ToArray();

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