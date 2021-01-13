using System;
using System.Linq;

namespace OSDP.Net.Model.ReplyData
{
    public class ExtendedRead
    {
        private ExtendedRead()
        {
        }

        public byte Mode { get; private set; }
        public byte PReply { get; private set; }
        public byte[] PData { get; private set; }

        internal static ExtendedRead ParseData(ReadOnlySpan<byte> data)
        {
            var dataArray = data.ToArray();
            if (dataArray.Length < 2)
            {
                // return;
                // throw new Exception("Invalid size for the data");
            }

            var extendedRead = new ExtendedRead
            {
                Mode = dataArray[0],
                PReply = dataArray.Length > 1 ? dataArray[1] : (byte)0,
                PData = dataArray.Length > 2 ? dataArray.Skip(2).ToArray() : new byte[]{}
            };

            return extendedRead;
        }
    }
}