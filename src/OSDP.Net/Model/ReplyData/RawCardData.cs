using System;
using System.Collections;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    public class RawCardData
    {
        private RawCardData()
        {
        }

        public byte ReaderNumber { get; private set; }
        public FormatCode FormatCode { get; private set;  }
        public ushort BitCount { get; private set; }
        public BitArray Data { get; private set; }

        internal static RawCardData ParseData(ReadOnlySpan<byte> data)
        {
            var dataArray = data.ToArray();
            if (dataArray.Length < 4)
            {
                throw new Exception("Invalid size for the data");
            }

            ushort bitCount = Message.ConvertBytesToUnsignedShort(new[] {dataArray[2], dataArray[3]});
            var cardData = new BitArray(dataArray.Skip(4).Take(dataArray.Length - 4).Reverse().ToArray());
            Reverse(cardData);
            cardData.Length = bitCount;

            var rawCardData = new RawCardData
            {
                ReaderNumber = dataArray[0],
                FormatCode = (FormatCode)dataArray[1],
                BitCount = bitCount,
                Data = cardData
            };

            return rawCardData;
        }

        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"Reader Number: {ReaderNumber}");
            build.AppendLine($"  Format Code: {Message.SplitCamelCase(FormatCode.ToString())}");
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