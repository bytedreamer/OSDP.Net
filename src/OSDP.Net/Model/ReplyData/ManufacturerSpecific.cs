using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// A manufacturer specific reply.
    /// </summary>
    public class ManufacturerSpecific
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="ManufacturerSpecific"/> class from being created.
        /// </summary>
        private ManufacturerSpecific()
        {
        }

        /// <summary>
        /// Gets the vendor code.
        /// </summary>
        public IEnumerable<byte> VendorCode { get; private set; }

        /// <summary>
        /// Gets the manufacture specific data.
        /// </summary>
        public IEnumerable<byte> Data { get; private set; }

        public static ManufacturerSpecific ParseData(ReadOnlySpan<byte> data)
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

        /// <inheritdoc />
        public override string ToString() => ToString(0);
        public string ToString(int indent = 0)
        {
            var padding = new string(' ', indent);
            var build = new StringBuilder();
            build.AppendLine($"{padding}Vendor Code: {BitConverter.ToString(VendorCode.ToArray())}");
            build.AppendLine($"{padding}       Data: {BitConverter.ToString(Data.ToArray())}");
            return build.ToString();
        }
    }
}