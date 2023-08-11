using System;
using OSDP.Net.Messages.SecureChannel;
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

        protected override byte CommandCode => (byte)CommandType.PivData;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithDataSecurity;
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