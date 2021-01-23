using System;
using System.Collections.Generic;

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

        internal IEnumerable<byte> BuildData()
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
    }
}
