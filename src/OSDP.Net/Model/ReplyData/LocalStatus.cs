using System;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    public class LocalStatus : ReplyData
    {
        private LocalStatus()
        {
        }

        public bool Tamper { get; private set;  }
        public bool PowerFailure { get; private set; }

        internal static LocalStatus CreateLocalStatus(Reply reply)
        {
            var data = reply.ExtractReplyData.ToArray();
            if (data.Length < 2)
            {
                throw new Exception("Invalid size for the data");
            }

            var localStatus = new LocalStatus
            {
                Tamper = Convert.ToBoolean(data[0]),
                PowerFailure = Convert.ToBoolean(data[1])
            };

            return localStatus;
        }

        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"       Tamper: {Tamper}");
            build.AppendLine($"Power Failure: {PowerFailure}");

            return build.ToString();
        }
    }
}