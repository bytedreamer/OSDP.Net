using System;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// The actual communication configuration of the PD sent as a reply.
    /// </summary>
    public class CommunicationConfiguration : PayloadData
    {
        /// <summary>
        /// Initializes a new instance of CommunicationConfiguration class
        /// </summary>
        /// <param name="address"></param>
        /// <param name="baudRate"></param>
        public CommunicationConfiguration(byte address, int baudRate)
        {
            Address = address;
            BaudRate = baudRate;
        }

        /// <inheritdoc/>
        public override byte Code => (byte)ReplyType.PdCommunicationsConfigurationReport;

        /// <summary>
        /// Gets the address.
        /// </summary>
        public byte Address { get; }

        /// <summary>
        /// Gets the baud rate.
        /// </summary>
        public int BaudRate { get; }

        /// <inheritdoc />
        public override ReadOnlySpan<byte> SecurityControlBlock() => SecurityBlock.ReplyMessageWithDataSecurity;

        /// <summary>Parses the message payload bytes</summary>
        /// <param name="data">Message payload as bytes</param>
        /// <returns>An instance of CommunicationConfiguration representing the message payload</returns>
        internal static CommunicationConfiguration ParseData(ReadOnlySpan<byte> data)
        {
            var dataArray = data.ToArray();
            if (dataArray.Length != 5)
            {
                throw new Exception("Invalid size for the data");
            }

            return new CommunicationConfiguration(dataArray[0], Message.ConvertBytesToInt(dataArray.Skip(1).ToArray()));
        }

        /// <inheritdoc/>
        public override byte[] BuildData() => [
            Address, (byte)(BaudRate & 0xff),(byte)((BaudRate >> 8) & 0xff),
            (byte)((BaudRate >> 16) & 0xff), (byte)((BaudRate >> 24) & 0xff)];

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