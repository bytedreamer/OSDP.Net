using System.Collections.Generic;

namespace OSDP.Net.Messages
{
    public class DeviceCapabilitiesCommand : Command
    {
        public DeviceCapabilitiesCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => 0x62;

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
            return new byte[] { 0x00 };
        }
    }
}