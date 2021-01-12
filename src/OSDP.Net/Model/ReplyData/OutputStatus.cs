using System;
using System.Collections.Generic;
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

        public IEnumerable<bool> OutputStatuses { get; private set; }

        internal static OutputStatus ParseData(ReadOnlySpan<byte> data)
        {
            return new OutputStatus {OutputStatuses = data.ToArray().Select(Convert.ToBoolean)};
        }

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