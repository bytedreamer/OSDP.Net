using System;

namespace OSDP.Net.Messages.ACU
{
    internal class DeviceCapabilitiesCommand : Command
    {
        public DeviceCapabilitiesCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => (byte)CommandType.DeviceCapabilities;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithDataSecurity;
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