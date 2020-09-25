using OSDP.Net.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace OSDP.Net.Model.CommandData
{
    public class ManufacturerSpecificCommandData 
    {
        public byte[] VendorCode { get; } = new byte[3];
        public byte[] Data { get; } 
        public ManufacturerSpecificCommandData(byte[] vendorCode, byte[] data)
        {
            
            if (vendorCode == null) throw new ArgumentNullException(nameof(vendorCode));
            else if (vendorCode.Length < 3) throw new ArgumentOutOfRangeException(nameof(vendorCode));

            if (data == null) throw new ArgumentNullException(nameof(data));
            else if (data.Length < 1) throw new ArgumentOutOfRangeException(nameof(data));


            VendorCode = vendorCode;
            Data = data;
         
        }

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
