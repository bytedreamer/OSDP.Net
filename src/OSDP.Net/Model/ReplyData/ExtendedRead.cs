using System.Linq;
using OSDP.Net.Messages;

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

        internal static ExtendedRead ParseData(Reply reply)
        {
            var data = reply.ExtractReplyData.ToArray();
            if (data.Length < 2)
            {
                // return;
                // throw new Exception("Invalid size for the data");
            }

            var extendedRead = new ExtendedRead
            {
                Mode = data[0],
                PReply = data.Length > 1 ? data[1] : (byte)0,
                PData = data.Length > 2 ? data.Skip(2).ToArray() : new byte[]{}
            };

            return extendedRead;
        }
    }
}