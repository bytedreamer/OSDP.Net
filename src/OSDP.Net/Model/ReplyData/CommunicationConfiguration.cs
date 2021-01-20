using System;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// The actual communication configuration of the PD sent as a reply.
    /// </summary>
    public class CommunicationConfiguration
    {
        private CommunicationConfiguration()
        {
        }

        /// <summary>
        /// Gets the address.
        /// </summary>
        public byte Address { get; private set; }

        /// <summary>
        /// Gets the baud rate.
        /// </summary>
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

        /// <inheritdoc />
        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"  Address: {Address}");
            build.AppendLine($"Baud Rate: {BaudRate}");

            return build.ToString();
        }
    }
}