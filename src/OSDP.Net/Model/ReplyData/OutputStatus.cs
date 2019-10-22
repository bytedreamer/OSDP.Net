using System;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    public class OutputStatus
    {
        private OutputStatus()
        {
        }

        public bool[] OutputStatuses { get; private set; }

        internal static OutputStatus ParseData(Reply reply)
        {
            var data = reply.ExtractReplyData.ToArray();

            return new OutputStatus {OutputStatuses = data.Select(Convert.ToBoolean).ToArray()};
        }

        public override string ToString()
        {
            byte outputNumber = 1;
            var build = new StringBuilder();
            foreach (bool outputStatus in OutputStatuses)
            {
                build.AppendLine($"Output Number {outputNumber++:00}: {outputStatus}");
            }

            return build.ToString();
        }
    }
}