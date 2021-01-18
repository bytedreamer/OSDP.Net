using System;
using System.Linq;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages
{
    internal class OutputControlCommand : Command
    {
        private readonly OutputControls _outputControls;

        public OutputControlCommand(byte address, OutputControls outputControls)
        {
            _outputControls = outputControls;
            Address = address;
        }

        protected override byte CommandCode => 0x68;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x02,
                0x17
            };
        }

        protected override ReadOnlySpan<byte> Data()
        {
            return _outputControls.BuildData().ToArray();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
            
        }
    }
}