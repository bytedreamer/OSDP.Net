using System.Collections.Generic;

namespace OSDP.Net.Messages
{
    public class PollCommand : Command
    {
        public PollCommand(byte address, Control control)
        {
            Address = address;
        }

        protected override byte CommandCode => 0x60;

        protected override IEnumerable<byte> SecurityControlBlock()
        {
            throw new System.NotImplementedException();
        }

        protected override IEnumerable<byte> Data()
        {
            return new byte[] { };
        }
    }
}