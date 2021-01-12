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

        internal static CommunicationConfiguration ParseData(ReadOnlySpan<byte> data)
        {
            var dataArray = data.ToArray();
            if (dataArray.Length != 5)
            {
                throw new Exception("Invalid size for the data");
            }

            return new CommunicationConfiguration
            {
                Address = dataArray[0],
                BaudRate = Message.ConvertBytesToInt(dataArray.Skip(1).ToArray())
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