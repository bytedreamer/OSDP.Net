using System.Collections.Generic;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages
{
    public class BuzzerControlCommand : Command
    {
        private readonly BuzzerControl _buzzerControl;

        public BuzzerControlCommand(byte address, BuzzerControl buzzerControl)
        {
            _buzzerControl = buzzerControl;
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
            return _buzzerControl.BuildData();
        }

        protected override void CustomCommandUpdate(List<byte> commandBuffer)
        {
            
        }
    }
}