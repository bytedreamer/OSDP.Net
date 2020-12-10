using System;
using System.Collections.Generic;

namespace OSDP.Net.Model.CommandData
{
    public class ManufacturerSpecificCommandData 
    {
        public ManufacturerSpecificCommandData(byte[] vendorCode, byte[] data)
        {
            VendorCode = vendorCode ?? throw new ArgumentNullException(nameof(vendorCode));
            if (VendorCode.Length != 3) throw new ArgumentOutOfRangeException(nameof(vendorCode));

            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public byte[] VendorCode { get; }

        public byte[] Data { get; }

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
    }
}
