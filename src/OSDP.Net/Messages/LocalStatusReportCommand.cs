using System.Collections.Generic;

namespace OSDP.Net.Messages
{
    public class LocalStatusReportCommand : Command
    {
        public LocalStatusReportCommand(byte address)
        {
            Address = address;
        }

        protected override byte CommandCode => 0x64;

        protected override IEnumerable<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x02,
                0x15
            };
        }

        protected override IEnumerable<byte> Data()
        {
            return new byte[] { };
        }
    }
}