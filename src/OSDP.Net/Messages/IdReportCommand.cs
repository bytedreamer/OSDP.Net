using System.Collections.Generic;

namespace OSDP.Net.Messages
{
    public class IdReportCommand : Command
    {
        public IdReportCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => 0x61;

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

        protected override void CustomCommandUpdate(List<byte> commandBuffer)
        {
            
        }
    }
}