using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    public class ReaderStatus
    {
        public enum ReaderTamperStatus
        {
            Normal = 0x00,
            NotConnected = 0x01,
            Tamper = 0x02
        }

        private ReaderStatus()
        {
        }

        public IEnumerable<ReaderTamperStatus> ReaderStatuses { get; private set; }

        internal static ReaderStatus ParseData(ReadOnlySpan<byte> data)
        {
            return new ReaderStatus
                {ReaderStatuses = data.ToArray().Select(status => (ReaderTamperStatus) status)};
        }

        public override string ToString()
        {
            byte readerNumber = 0;
            var build = new StringBuilder();
            foreach (var readerStatuses in ReaderStatuses)
            {
                build.AppendLine($"Reader Number {readerNumber++:00}: {Message.SplitCamelCase(readerStatuses.ToString())}");
            }

            return build.ToString();
        }
    }
}