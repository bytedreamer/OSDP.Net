using System.Collections.Generic;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages
{
    public class OutputControlCommand : Command
    {
        private readonly OutputControls _outputControls;

        public OutputControlCommand(byte address, OutputControls outputControls)
        {
            _outputControls = outputControls;
            Address = address;
        }

        protected override byte CommandCode => 0x68;

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
            return _outputControls.BuildData();
        }

        protected override void CustomCommandUpdate(List<byte> commandBuffer)
        {
            
        }
    }
}