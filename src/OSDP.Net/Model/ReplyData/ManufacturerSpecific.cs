using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSDP.Net.Model.ReplyData
{
    public class ManufacturerSpecific
    {
        private ManufacturerSpecific()
        {
        }

        public IEnumerable<byte> VendorCode { get; private set; }

        public IEnumerable<byte> Data { get; private set; }

        internal static ManufacturerSpecific ParseData(ReadOnlySpan<byte> data)
        {
            var dataArray = data.ToArray();
            if (dataArray.Length < 3)
            {
                throw new Exception("Invalid size for the data");
            }

            var manufacturerSpecificReply = new ManufacturerSpecific
            {
                VendorCode = dataArray.Take(3),
                Data = dataArray.Length > 3 ? dataArray.Skip(3).ToArray() : null
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