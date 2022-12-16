using System;

namespace OSDP.Net.Messages.ACU
{
    internal class DeviceCapabilitiesCommand : Command
    {
        public DeviceCapabilitiesCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => 0x62;

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
            return new byte[] { 0x00 };
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {

        }
    }
}