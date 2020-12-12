using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    public class ManufacturerSpecificData
    {
        private ManufacturerSpecificData()
        {
        }

        public IEnumerable<byte> VendorCode { get; private set; }

        public IEnumerable<byte> Data { get; private set; }

        internal static ManufacturerSpecificData ParseData(Reply reply)
        {
            var data = reply.ExtractReplyData.ToArray();
            if (data.Length < 3)
            {
                throw new Exception("Invalid size for the data");
            }

            var manufacturerSpecificReply = new ManufacturerSpecificData
            {
                VendorCode = data.Take(3),
                Data = data.Length > 3 ? data.Skip(3).ToArray() : null
            };

            return manufacturerSpecificReply;
        }

        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"Vendor Code: {BitConverter.ToString(VendorCode.ToArray())}");
            build.AppendLine($"       Data: {BitConverter.ToString(Data.ToArray())}");
            return build.ToString();
        }
    }
}