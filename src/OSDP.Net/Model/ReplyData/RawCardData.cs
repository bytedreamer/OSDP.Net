using System;
using System.Collections;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    public class RawCardData : ReplyData
    {
        private RawCardData()
        {
        }

        public byte ReaderNumber { get; private set; }
        public FormatCode FormatCode { get; private set;  }
        public ushort BitCount { get; private set; }
        public BitArray Data { get; private set; }

        internal static RawCardData CreateRawCardData(Reply reply)
        {
            var data = reply.ExtractReplyData.ToArray();
            if (data.Length < 4)
            {
                throw new Exception("Invalid size for the data");
            }

            ushort bitCount = Message.ConvertBytesToShort(new[] {data[2], data[3]});
            var cardData = new BitArray(data.Skip(4).Take(data.Length - 4).Reverse().ToArray());
            Reverse(cardData);
            cardData.Length = bitCount;

            var rawCardData = new RawCardData
            {
                ReaderNumber = data[0],
                FormatCode = (FormatCode)data[1],
                BitCount = bitCount,
                Data = cardData
            };

            return rawCardData;
        }

        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"Reader Number: {ReaderNumber}");
            build.AppendLine($"  Format Code: {SplitCamelCase(FormatCode.ToString())}");
            build.AppendLine($"    Bit Count: {BitCount}");
            build.AppendLine($"         Data: {FormatData(Data)}");
            return build.ToString();
        }

        private static string FormatData(BitArray bitArray)
        {
            var builder = new StringBuilder();
            foreach (bool bit in bitArray)
            {
                builder.Append(bit ? "1" : "0");
            }

            return builder.ToString();
        }

        private static void Reverse(BitArray array)
        {
            int length = array.Length;
            int mid = length / 2;

            for (int index = 0; index < mid; index++)
            {
                bool bit = array[index];
                array[index] = array[length - index - 1];
                array[length - index - 1] = bit;
            }    
        }
    }

    public enum FormatCode
    {
        NotSpecified = 0x0,
        Wiegand = 0x1
    }
}