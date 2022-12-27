using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSDP.Net.Utilities.Binary
{
    /// <summary>
    /// Helper routines for manipulating binary representations of data that are used
    /// within OSDP.NET library and its unit tests
    /// </summary>
    public static class BinaryUtils
    {
        /// <summary>
        /// Convert a string containing hex digits into BitArray
        /// </summary>
        /// <param name="hex">String to be converted</param>
        /// <returns>BitArray instance with most-significant bit first order</returns>
        public static BitArray HexStringToBitArray(string hex) => 
            HexStringToBitArray((IEnumerable<char>)hex);

        /// <summary>
        /// Convert a sequence of hex digit characters into BitArray
        /// </summary>
        /// <param name="hex">Character sequence to be converted</param>
        /// <returns>BitArray instance with most-significant bit first order</returns>
        public static BitArray HexStringToBitArray(IEnumerable<char> hex) => 
            new(HexToBytes(hex).Select(x => x.ReverseBits()).ToArray());

        /// <summary>
        /// Convert a sequence of hex digit characters into their byte representation
        /// </summary>
        /// <param name="seq">character digit sequence to be converted</param>
        /// <returns>Sequence of bytes</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if an invalid character is encountered
        /// </exception>
        public static IEnumerable<byte> HexToBytes(IEnumerable<char> seq)
        {
            var enumerator = seq.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var a = enumerator.Current.HexToInt();
                if (!enumerator.MoveNext()) throw new InvalidOperationException("Not a valid hex string");
                var b = enumerator.Current.HexToInt();

                // .NET's BitArray reads array of bytes in Least Significant Bit (LSB) first.
                //  However, the way we use this thing and to make it compatible with human-
                // readable hex strings, we want to store data as MSB first.
                yield return ((byte)((a << 4) | b));
            }
        }

        /// <summary>
        /// Convert BitArray instance to a binary string representation
        /// </summary>
        /// <param name="arr">BitArray to be converted</param>
        /// <returns>A string consisting almost entirely of 0s and 1s</returns>
        public static string BitArrayToString(BitArray arr)
        {
            var builder = new StringBuilder();
            foreach (bool bit in arr)
            {
                builder.Append(bit ? "1" : "0");
            }

            return builder.ToString();
        }
    }

    /// <summary>
    /// Extension methods for helping with binary conversion
    /// </summary>
    public static class BinaryExtensions
    {
        /// <summary>
        /// Reverse the bits of a single byte
        /// </summary>
        /// <param name="value">byte to be converted</param>
        /// <returns>Same byte with its bits reversed</returns>
        public static byte ReverseBits(this byte value)
        {
            int c = (byte)((value >> 4) | ((value & 0xf) << 4));
            c = ((c & 0xcc) >> 2) | ((c & 0x33) << 2);
            c = ((c & 0xaa) >> 1) | ((c & 0x55) << 1);
            return (byte)c;
        }

        /// <summary>
        /// Tests to see if character is a valid hex digit
        /// </summary>
        /// <param name="c">value to be tested</param>
        /// <returns>true if valid hex; false otherwise</returns>
        public static bool IsHex(this char c) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F');

        /// <summary>
        /// Converts a single hex digit character to its integer representation
        /// </summary>
        /// <param name="value">Character to be converted</param>
        /// <returns>Integer representation of the input character</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static int HexToInt(this char value) => value switch
        {
            char c when (c >= '0' && c <= '9') => (byte)(c - '0'),
            char c when (c >= 'A' && c <= 'F') => (byte)(c - 'A' + 10),
            char c when (c >= 'a' && c <= 'f') => (byte)(c - 'a' + 10),
            _ => throw new InvalidOperationException("Not a valid hex string")
        };
    }
}
