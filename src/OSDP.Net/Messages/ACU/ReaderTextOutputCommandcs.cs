using System;
using System.Linq;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages.ACU
{
    internal class ReaderTextOutputCommand : Command
    {
        private readonly ReaderTextOutput _readerTextOutput;

        public ReaderTextOutputCommand(byte address, ReaderTextOutput readerTextOutput)
        {
            _readerTextOutput = readerTextOutput;
            Address = address;
        }

        protected override byte CommandCode => (byte)CommandType.TextOutput;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithDataSecurity;
        }

        protected override ReadOnlySpan<byte> Data()
        {
            return _readerTextOutput.BuildData().ToArray();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {

        }
    }
}