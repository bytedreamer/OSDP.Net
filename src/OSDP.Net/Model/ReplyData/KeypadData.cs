using System;
using System.Text;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// A keypad reply.
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
        /// <para>Data returned from keypad</para>
        /// <para>The key encoding uses the following data representation:</para>
        /// <list type="bullet">
        ///     <item>
        ///         <description>Digits 0 through 9 are reported as ASCII characters 0x30 through 0x39</description>
        ///     </item>
        ///     <item>
        ///         <description>The clear/delete/'*' key is reported as ASCII DELETE, 0x7F</description>
        ///     </item>
        ///     <item>
        ///         <description>The enter/'#' key is reported as ASCII return, 0x0D</description>
        ///     </item>
        /// </list>
        /// <para>Special/function keys are reported as upper case ASCII:</para>
        /// <list type="bullet">
        ///     <item>
        ///         <description>A or F1 = 0x41</description>
        ///     </item>
        ///     <item>
        ///         <description>B or F2 = 0x42</description>
        ///     </item>
        ///     <item>
        ///         <description>C or F3 = 0x43</description>
        ///     </item>
        ///     <item>
        ///         <description>D or F4 = 0x44</description>
        ///     </item>
        ///     <item>
        ///         <description>F1 and F2 = 0x45</description>
        ///     </item>
        ///     <item>
        ///         <description>F2 and F3 = 0x46</description>
        ///     </item>
        ///     <item>
        ///         <description>F3 and F4 = 0x47</description>
        ///     </item>
        ///     <item>
        ///         <description>F1 and F4 = 0x48</description>
        ///     </item>
        /// </list>
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>Parses the message payload bytes</summary>
        /// <param name="data">Message payload as bytes</param>
        /// <returns>An instance of KeypadData representing the message payload</returns>
        public static KeypadData ParseData(ReadOnlySpan<byte> data)
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

        /// <inheritdoc/>
        public override string ToString() => ToString(0);

        /// <summary>
        /// Returns a string representation of the current object
        /// </summary>
        /// <param name="indent">Number of ' ' chars to add to beginning of every line</param>
        /// <returns>String representation of the current object</returns>
        public string ToString(int indent)
        {
            var padding = new string(' ', indent);
            var build = new StringBuilder();
            build.AppendLine($"{padding}Reader Number: {ReaderNumber}");
            build.AppendLine($"{padding}  Digit Count: {DigitCount}");
            build.AppendLine($"{padding}         Data: {DetermineCharacters(Data, DigitCount)}");
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
