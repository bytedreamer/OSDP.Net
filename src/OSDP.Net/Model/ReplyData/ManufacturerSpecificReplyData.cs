using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    public class ManufacturerSpecificReplyData
    {
        private ManufacturerSpecificReplyData()
        {
        }

        public IEnumerable<byte> VendorCode { get; protected set; }
        public IEnumerable<byte> Data { get; protected set; }

        internal static ManufacturerSpecificReplyData ParseData(Reply reply)
        {
            if (reply.Type != ReplyType.Ack)
            {

                var data = reply.ExtractReplyData.ToArray();
                if (data.Length < 3)
                {
                    throw new Exception("Invalid size for the data");
                }

                var manufacturerSpecificReply = new ManufacturerSpecificReplyData
                {
                    VendorCode = data.Take(3),
                    Data = data.Length > 3 ? data.Skip(3).ToArray() : null
                };

                return manufacturerSpecificReply;
            }
            else
                return new ManufacturerSpecificReplyData();
        }

        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"     Vendor Code: {BitConverter.ToString(VendorCode.ToArray())}");
            build.AppendLine($"     Data: {BitConverter.ToString(Data.ToArray())}");
            return build.ToString();
        }
    }
}