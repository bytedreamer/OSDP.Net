using System;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages
{
    internal class FileTransferCommand : Command
    {
        private readonly FileTransfer _fileTransfer;

        public FileTransferCommand(byte address, FileTransfer fileTransfer)
        {
            Address = address;
            _fileTransfer = fileTransfer;
        }

        protected override byte CommandCode => 0x7C;

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
            return _fileTransfer.BuildData().ToArray();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {

        }
    }
}