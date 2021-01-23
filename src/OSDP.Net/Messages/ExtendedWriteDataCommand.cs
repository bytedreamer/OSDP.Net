using System;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages
{
    internal class ExtendedWriteDataCommand : Command
    {
        private readonly ExtendedWrite _extendedWrite;

        public ExtendedWriteDataCommand(byte address, ExtendedWrite extendedWrite)
        {
            _extendedWrite = extendedWrite;
            Address = address;
        }

        protected override byte CommandCode => 0xA1;

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
            return _extendedWrite.BuildData();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }
    }
}