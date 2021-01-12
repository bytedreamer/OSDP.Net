using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    public class PIVData
    {
        private PIVData()
        {
        }

        public ushort WholeMessageLength { get; private set; }

        public ushort Offset { get; private set; }

        public ushort LengthOfFragment { get; private set; }

        public IEnumerable<byte> Data { get; private set; }

        internal static PIVData ParseData(ReadOnlySpan<byte> data)
        {
            if (data.Length < 6)
            {
                throw new Exception("Invalid size for the data");
            }

            var pivData = new PIVData
            {
                WholeMessageLength = Message.ConvertBytesToShort(data.Slice(0, 2)),
                Offset = Message.ConvertBytesToShort(data.Slice(2, 2)),
                LengthOfFragment = Message.ConvertBytesToShort(data.Slice(4, 2)),
                Data = data.Slice(6, data.Length - 6).ToArray()
            };

            return pivData;
        }

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