using System.Collections.Generic;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages
{
    public class ReaderBuzzerControlCommand : Command
    {
        private readonly ReaderBuzzerControl _readerBuzzerControl;

        public ReaderBuzzerControlCommand(byte address, ReaderBuzzerControl readerBuzzerControl)
        {
            _readerBuzzerControl = readerBuzzerControl;
            Address = address;
        }

        protected override byte CommandCode => 0x6A;

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
            return _readerBuzzerControl.BuildData();
        }

        protected override void CustomCommandUpdate(List<byte> commandBuffer)
        {
            
        }
    }
}