using System.Collections.Generic;

namespace OSDP.Net.Messages
{
    public class ReaderStatusReportCommand : Command
    {
        public ReaderStatusReportCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => 0x67;

        protected override IEnumerable<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x02,
                0x15
            };
        }

        protected override void CustomCommandUpdate(List<byte> commandBuffer)
        {
        }

        protected override IEnumerable<byte> Data()
        {
            return new byte[] { };
        }
    }
}