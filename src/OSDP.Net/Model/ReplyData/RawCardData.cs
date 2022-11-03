using System;
using System.Collections;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// A raw card data reply.
    /// </summary>
    public class RawCardData
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="RawCardData"/> class from being created.
        /// </summary>
        private RawCardData()
        {
        }

        /// <summary>
        /// The reader number.
        /// </summary>
        public byte ReaderNumber { get; private set; }
        /// <summary>
        /// The card format code.
        /// </summary>
        public FormatCode FormatCode { get; private set;  }
        /// <summary>
        /// The bit count returned by the card.
        /// </summary>
        public ushort BitCount { get; private set; }
        /// <summary>
        /// The raw card data.
        /// </summary>
        public BitArray Data { get; private set; }

        /// <summary>
        /// Parses the data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>RawCardData.</returns>
        /// <exception cref="System.Exception">Invalid size for the data</exception>
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

        /// <inheritdoc />
        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"Reader Number: {ReaderNumber}");
            build.AppendLine($"  Format Code: {Helpers.SplitCamelCase(FormatCode.ToString())}");
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

    /// <summary>
    /// The raw card format code.
    /// </summary>
    public enum FormatCode
    {
        /// <summary>
        /// Card format not specified
        /// </summary>
        NotSpecified = 0x0,
        /// <summary>
        /// Wiegand card format
        /// </summary>
        Wiegand = 0x1
    }
}