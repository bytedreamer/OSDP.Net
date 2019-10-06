using System;
using System.Collections.Generic;

namespace OSDP.Net.Model.ReplyData
{
    public abstract class ReplyData
    {
        protected static int ConvertBytesToInt(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return BitConverter.ToInt32(bytes, 0);
        }

        protected static IEnumerable<byte> ConvertIntToBytes(int value)
        {
            var byteArray = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteArray);
            }

            return byteArray;
        }
    }
}