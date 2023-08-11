using System;
using System.Linq;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages.ACU
{
    internal class ReaderBuzzerControlCommand : Command
    {
        private readonly ReaderBuzzerControl _readerBuzzerControl;

        public ReaderBuzzerControlCommand(byte address, ReaderBuzzerControl readerBuzzerControl)
        {
            _readerBuzzerControl = readerBuzzerControl;
            Address = address;
        }

        protected override byte CommandCode => (byte)CommandType.BuzzerControl;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithDataSecurity;
        }

        protected override ReadOnlySpan<byte> Data()
        {
            return _readerBuzzerControl.BuildData().ToArray();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {

        }
    }
}