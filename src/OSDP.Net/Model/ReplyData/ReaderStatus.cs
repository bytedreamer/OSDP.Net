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

        public ReaderTamperStatus[] ReaderStatuses { get; private set; }

        internal static ReaderStatus ParseData(Reply reply)
        {
            var data = reply.ExtractReplyData.ToArray();

            return new ReaderStatus {ReaderStatuses = data.Select(status => (ReaderTamperStatus) status).ToArray()};
        }

        public override string ToString()
        {
            byte readerNumber = 1;
            var build = new StringBuilder();
            foreach (ReaderTamperStatus readerStatuses in ReaderStatuses)
            {
                build.AppendLine($"Reader Number {readerNumber++:00}: {Message.SplitCamelCase(readerStatuses.ToString())}");
            }

            return build.ToString();
        }
    }
}