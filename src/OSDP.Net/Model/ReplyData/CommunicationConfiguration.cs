using System;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    public class CommunicationConfiguration
    {
        private CommunicationConfiguration()
        {
        }

        public byte Address { get; private set; }
        public int BaudRate { get; private set; }

        internal static CommunicationConfiguration ParseData(Reply reply)
        {
            var data = reply.ExtractReplyData.ToArray();
            if (data.Length != 5)
            {
                throw new Exception("Invalid size for the data");
            }

            return new CommunicationConfiguration
            {
                Address = data[0],
                BaudRate = Message.ConvertBytesToInt(data.Skip(1).ToArray())
            };
        }

        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"  Address: {Address}");
            build.AppendLine($"Baud Rate: {BaudRate}");

            return build.ToString();
        }
    }
}