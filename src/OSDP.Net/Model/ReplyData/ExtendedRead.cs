using System;
using System.Linq;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// Reply data for smart card handling on a PD.
    /// </summary>
    public class ExtendedRead
    {
        private ExtendedRead()
        {
        }

        /// <summary>
        /// Gets the extended READ/WRITE Mode.
        /// </summary>
        public byte Mode { get; private set; }

        /// <summary>
        /// Gets the reply code dependent on command.
        /// </summary>
        public byte PReply { get; private set; }

        /// <summary>
        /// Gets the data dependent on command.
        /// </summary>
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