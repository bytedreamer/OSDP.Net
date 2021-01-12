using System;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    public class LocalStatus
    {
        private LocalStatus()
        {
        }

        public bool Tamper { get; private set;  }
        public bool PowerFailure { get; private set; }

        internal static LocalStatus ParseData(ReadOnlySpan<byte> data)
        {
            var dataArray = data.ToArray();
            if (dataArray.Length < 2)
            {
                throw new Exception("Invalid size for the data");
            }

            var localStatus = new LocalStatus
            {
                Tamper = Convert.ToBoolean(dataArray[0]),
                PowerFailure = Convert.ToBoolean(dataArray[1])
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