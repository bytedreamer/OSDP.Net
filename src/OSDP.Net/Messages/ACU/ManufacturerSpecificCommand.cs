using System;
using OSDP.Net.Model.CommandData;
using System.Linq;

namespace OSDP.Net.Messages.ACU
{
    internal class ManufacturerSpecificCommand : Command
    {
        private readonly ManufacturerSpecific _manufacturerData;

        public ManufacturerSpecificCommand(byte address, ManufacturerSpecific manufacturerData)
        {
            Address = address;
            _manufacturerData = manufacturerData;
        }

        protected override byte CommandCode => 0x80;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x02,
                0x17
            };
        }

        protected override ReadOnlySpan<byte> Data()
        {
            return _manufacturerData.BuildData().ToArray();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {

        }
    }
}
