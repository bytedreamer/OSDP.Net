using System;
using OSDP.Net.Model.CommandData;
using System.Linq;
using OSDP.Net.Messages.SecureChannel;

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

        protected override byte CommandCode => (byte)CommandType.ManufacturerSpecific;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithDataSecurity;
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
