using System;
using System.Linq;
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

        protected override byte CommandCode => 0x6A;

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
            return _readerBuzzerControl.BuildData().ToArray();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {

        }
    }
}