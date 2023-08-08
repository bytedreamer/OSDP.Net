using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Manufacture specific command data sent to a PD.
    /// </summary>
    public class ManufacturerSpecific 
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ManufacturerSpecific"/> class.
        /// </summary>
        /// <param name="vendorCode">The vendor code.</param>
        /// <param name="data">The manufacture specific data.</param>
        /// <exception cref="ArgumentNullException">vendorCode</exception>
        /// <exception cref="ArgumentNullException">data</exception>
        /// <exception cref="ArgumentOutOfRangeException">vendorCode</exception>
        public ManufacturerSpecific(byte[] vendorCode, byte[] data)
        {
            VendorCode = vendorCode ?? throw new ArgumentNullException(nameof(vendorCode));
            if (VendorCode.Length != 3) throw new ArgumentOutOfRangeException(nameof(vendorCode));

            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>
        /// Gets the vendor code.
        /// </summary>
        public byte[] VendorCode { get; }

        /// <summary>
        /// Gets the manufacture specific data.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>Parses the message payload bytes</summary>
        /// <param name="data">Message payload as bytes</param>
        /// <returns>An instance of ManufacturerSpecific representing the message payload</returns>
        public static ManufacturerSpecific ParseData(ReadOnlySpan<byte> data)
        {
            return new ManufacturerSpecific(
                data.Slice(0, 3).ToArray(),
                data.Slice(3).ToArray()
            );
        }

        /// <summary>
        /// Builds the data.
        /// </summary>
        /// <returns>The Data</returns>
        public IEnumerable<byte> BuildData()
        {
            var data = new List<byte>
            {
                VendorCode[0],
                VendorCode[1],
                VendorCode[2]
            };
            data.AddRange(Data);
            return data;
        }

        /// <inheritdoc/>
        public override string ToString() => ToString(0);

        /// <summary>
        /// Returns a string representation of the current object
        /// </summary>
        /// <param name="indent">Number of ' ' chars to add to beginning of every line</param>
        /// <returns>String representation of the current object</returns>
        public string ToString(int indent)
        {
            var padding = new string(' ', indent);
            var build = new StringBuilder();
            build.AppendLine($"{padding}Vendor Code: {BitConverter.ToString(VendorCode.ToArray())}");
            build.AppendLine($"{padding}       Data: {BitConverter.ToString(Data.ToArray())}");
            return build.ToString();
        }
    }
}
