using System;

namespace OSDP.Net.Utilities
{
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
            { } c when (c >= '0' && c <= '9') => (byte)(c - '0'),
            { } c when (c >= 'A' && c <= 'F') => (byte)(c - 'A' + 10),
            { } c when (c >= 'a' && c <= 'f') => (byte)(c - 'a' + 10),
            _ => throw new InvalidOperationException("Not a valid hex string")
        };
    }
}
