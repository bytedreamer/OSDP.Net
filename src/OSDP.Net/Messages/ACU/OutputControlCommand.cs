using System;
using System.Linq;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages.ACU
{
    internal class OutputControlCommand : Command
    {
        private readonly OutputControls _outputControls;

        public OutputControlCommand(byte address, OutputControls outputControls)
        {
            _outputControls = outputControls;
            Address = address;
        }

        protected override byte CommandCode => (byte)CommandType.OutputControl;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithDataSecurity;
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