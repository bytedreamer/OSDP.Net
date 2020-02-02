using System.Collections.Generic;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages
{
    public class ReaderTextOutputCommand : Command
    {
        private readonly ReaderTextOutput _readerTextOutput;

        public ReaderTextOutputCommand(byte address, ReaderTextOutput readerTextOutput)
        {
            _readerTextOutput = readerTextOutput;
            Address = address;
        }

        protected override byte CommandCode => 0x6B;

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
            return _readerTextOutput.BuildData();
        }

        protected override void CustomCommandUpdate(List<byte> commandBuffer)
        {
            
        }
    }
}