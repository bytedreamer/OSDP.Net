using System;
using System.Linq;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages.ACU
{
    internal class ReaderLedControlCommand : Command
    {
        private readonly ReaderLedControls _readerLedControls;

        public ReaderLedControlCommand(byte address, ReaderLedControls readerLedControls)
        {
            _readerLedControls = readerLedControls;
            Address = address;
        }

        protected override byte CommandCode => (byte)CommandType.LEDControl;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithDataSecurity;
        }

        protected override ReadOnlySpan<byte> Data()
        {
            return _readerLedControls.BuildData().ToArray();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }
    }
}