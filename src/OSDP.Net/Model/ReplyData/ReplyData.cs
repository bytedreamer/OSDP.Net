using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

        protected static string SplitCamelCase( string str )
        {
            return Regex.Replace( 
                Regex.Replace( 
                    str, 
                    @"(\P{Ll})(\P{Ll}\p{Ll})", 
                    "$1 $2" 
                ), 
                @"(\p{Ll})(\P{Ll})", 
                "$1 $2" 
            );
        }
    }
}