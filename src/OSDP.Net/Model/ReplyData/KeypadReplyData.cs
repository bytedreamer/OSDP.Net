using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace OSDP.Net.Model.ReplyData
{
    public class KeypadReplyData
    {
        private const int ReplyKeyPadDataLength = 2;

        public byte ReaderNumber { get; private set; }
        public ushort BitCount { get; private set; }
        public BitArray Data { get; private set; }

        internal static KeypadReplyData ParseData(ReadOnlySpan<byte> data)
        {
            var dataArray = data.ToArray();
            if (dataArray.Length < ReplyKeyPadDataLength)
            {
                throw new Exception("Invalid size for the data");
            }

            var keypadData = new BitArray(dataArray.Skip(ReplyKeyPadDataLength)
                .Take(dataArray.Length - ReplyKeyPadDataLength).Reverse().ToArray());
            Reverse(keypadData);
            var keypadReplyData = new KeypadReplyData
            {
                ReaderNumber = dataArray[0],
                BitCount = dataArray[1],
                Data = keypadData
            };

            return keypadReplyData;
        }

        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"Reader Number: {ReaderNumber}");
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
}
