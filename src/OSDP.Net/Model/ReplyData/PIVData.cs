using System;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// A PIV data reply.
    /// </summary>
    public class PIVData
    {
        private PIVData()
        {
        }

        /// <summary>
        /// The length of the whole message.
        /// </summary>
        public ushort WholeMessageLength { get; private set; }

        /// <summary>
        /// The offset.
        /// </summary>
        public ushort Offset { get; private set; }

        /// <summary>
        /// The length of fragment.
        /// </summary>
        public ushort LengthOfFragment { get; private set; }

        /// <summary>
        /// The data.
        /// </summary>
        public byte[] Data { get; private set; }

        internal static PIVData ParseData(ReadOnlySpan<byte> data)
        {
            if (data.Length < 6)
            {
                // throw new Exception("Invalid size for the data");
                return new PIVData {Data = new byte[] { }};
            }

            var pivData = new PIVData
            {
                WholeMessageLength = Message.ConvertBytesToUnsignedShort(data.Slice(0, 2)),
                Offset = Message.ConvertBytesToUnsignedShort(data.Slice(2, 2)),
                LengthOfFragment = Message.ConvertBytesToUnsignedShort(data.Slice(4, 2)),
                Data = data.Slice(6, data.Length - 6).ToArray()
            };

            return pivData;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"Whole Message Length: {WholeMessageLength}");
            build.AppendLine($"              Offset: {Offset}");
            build.AppendLine($"  Length of Fragment: {LengthOfFragment}");
            build.AppendLine($"                Data: {BitConverter.ToString(Data.ToArray())}");
            return build.ToString();
        }
    }
}