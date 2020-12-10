using OSDP.Net.Model.CommandData;
using System.Collections.Generic;

namespace OSDP.Net.Messages
{
    public class ManufacturerSpecificCommand : Command
    {
        private readonly ManufacturerSpecific _manufacturerData;

        public ManufacturerSpecificCommand(byte address, ManufacturerSpecific manufacturerData)
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
