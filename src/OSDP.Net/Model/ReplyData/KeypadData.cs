using System;
using System.Text;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// Keypad data entered on PD
    /// </summary>
    public class KeypadData
    {
        private const int DataStartIndex = 2;

        /// <summary>
        /// Reader number 0=First Reader 1=Second Reader
        /// </summary>
        public byte ReaderNumber { get; private set; }

        /// <summary>
        /// Number of digits in the return data
        /// </summary>
        public ushort DigitCount { get; private set; }

        /// <summary>
        /// Data returned from keypad
        /// 
        /// The key encoding uses the following data representation:
        /// Digits 0 through 9 are reported as ASCII characters 0x30 through 0x39
        /// The clear/delete/'*' key is reported as ASCII DELETE, 0x7F
        /// The enter/'#' key is reported as ASCII return, 0x0D
        /// Special/function keys are reported as upper case ASCII:
        /// A or F1 = 0x41, B or F2 = 0x42, C or F3 = 0x43, D or F4 = 0x44
        /// F1 & F2 = 0x45, F2 & F3 = 0x46, F3 & F4 = 0x47, F1 & F4 = 0x48
        /// </summary>
        public byte[] Data { get; private set; }

        internal static KeypadData ParseData(ReadOnlySpan<byte> data)
        {
            if (data.Length < DataStartIndex)
            {
                throw new Exception("Invalid size for the data");
            }

            var keypadReplyData = new KeypadData
            {
                ReaderNumber = data[0],
                DigitCount = data[1],
                Data = data.Slice(DataStartIndex, data.Length - DataStartIndex).ToArray()
            };

            return keypadReplyData;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"Reader Number: {ReaderNumber}");
            build.AppendLine($"  Digit Count: {DigitCount}");
            build.AppendLine($"         Data: {DetermineCharacters(Data, DigitCount)}");
            return build.ToString();
        }

        private string DetermineCharacters(byte[] data, ushort count)
        {
            var build = new StringBuilder();
            for (byte index = 0; index < count; index++)
            {
                switch (data[index])
                {
                    case 0x0D:
                        build.Append('#');
                        break;
                    case 0x7F:
                        build.Append('*');
                        break;
                    default:
                        build.Append(Convert.ToChar(data[index]));
                        break;
                }
            }

            return build.ToString();
        }
    }
}
