using System.Collections.Generic;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages
{
    public class ReaderLedControlCommand : Command
    {
        private readonly ReaderLedControls _readerLedControls;

        public ReaderLedControlCommand(byte address, ReaderLedControls readerLedControls)
        {
            _readerLedControls = readerLedControls;
            Address = address;
        }

        protected override byte CommandCode => 0x69;

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
            return _readerLedControls.BuildData();
        }

        protected override void CustomCommandUpdate(List<byte> commandBuffer)
        {
            
        }
    }
}