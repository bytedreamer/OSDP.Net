using System;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages.ACU
{
    internal class GetPIVDataCommand : Command
    {
        private readonly GetPIVData _getPivData;

        public GetPIVDataCommand(byte address, GetPIVData getPivData)
        {
            _getPivData = getPivData;
            Address = address;
        }

        protected override byte CommandCode => 0xA3;

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
            return _getPivData.BuildData();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }
    }
}