using OSDP.Net.Model.CommandData;
using System;
using System.Collections.Generic;
using System.Text;

namespace OSDP.Net.Messages
{
    public class ManufacturerSpecificCommand : Command
    {
        private ManufacturerSpecificCommandData _manufacturerData;
        public ManufacturerSpecificCommand(byte address, ManufacturerSpecificCommandData manufacturerData)
        {
            Address = address;
            _manufacturerData = manufacturerData;
        }

        protected override byte CommandCode => 0x80;

        protected override IEnumerable<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x02,
                0x17
            };
        }
        
        protected override IEnumerable<byte> Data()
        {
            return _manufacturerData.BuildData();
        }

        protected override void CustomCommandUpdate(List<byte> commandBuffer)
        {

        }
    }
}
