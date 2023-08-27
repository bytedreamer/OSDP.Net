using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using OSDP.Net.Messages;

[assembly: InternalsVisibleTo("OSDP.Net.Tests.Model.ReplyData")]
namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// A raw card data reply.
    /// </summary>
    public class RawCardData : ReplyData
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="RawCardData"/> class from being created.
        /// </summary>
        private RawCardData()
        {
        }

        /// <summary>
        /// Creates a new instance of RawCardData. The parameters passed here are
        /// defined in OSDP spec for osdp_RAW response
        /// </summary>
        /// <param name="readerNumber">Reader number</param>
        /// <param name="format">Format code</param>
        /// <param name="data">Data</param>
        public RawCardData(byte readerNumber, FormatCode format, BitArray data)
        {
            ReaderNumber = readerNumber;
            FormatCode = format;
            Data = data;
            BitCount = (ushort)data.Count;
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
        /// <inheritdoc/>
        public override ReplyType ReplyType => ReplyType.RawReaderData;

        /// <summary>
        /// Parses the data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>RawCardData.</returns>
        /// <exception cref="System.Exception">Invalid size for the data</exception>
        public static RawCardData ParseData(ReadOnlySpan<byte> data)
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

        /// <inheritdoc/>
        public override string ToString(int indent)
        {
            var padding = new string(' ', indent);
            var build = new StringBuilder();
            build.AppendLine($"{padding}Reader Number: {ReaderNumber}");
            build.AppendLine($"{padding}  Format Code: {Helpers.SplitCamelCase(FormatCode.ToString())}");
            build.AppendLine($"{padding}    Bit Count: {BitCount}");
            build.AppendLine($"{padding}         Data: {FormatData(Data)}");
            return build.ToString();
        }

        internal static string FormatData(BitArray bitArray)
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
                (array[index], array[length - index - 1]) = (array[length - index - 1], array[index]);
            }    
        }

        /// <inheritdoc/>
        public override byte[] BuildData()
        {
            var length = 4 + (Data.Count + 7) / 8;
            var buffer = new byte[length];
            buffer[0] = ReaderNumber;
            buffer[1] = (byte)FormatCode;
            buffer[2] = (byte)(BitCount & 0xff);
            buffer[3] = (byte)((BitCount >> 8) & 0xff);
            Data.CopyTo(buffer, 4);

            for (int i = 4; i < length; i++)
            {
                // Source of this nifty little bitwise logic to reverse the bits:
                // http://igoro.com/archive/programming-job-interview-challenge/

                byte b = buffer[i];
                int c = (byte)((b >> 4) | ((b & 0xf) << 4));
                c = ((c & 0xcc) >> 2) | ((c & 0x33) << 2);
                c = ((c & 0xaa) >> 1) | ((c & 0x55) << 1);
                buffer[i] = (byte)c;
            }

            return buffer;
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