using System;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages.ACU
{
    internal class FileTransferCommand : Command
    {
        private readonly FileTransferFragment _fileTransferFragment;

        public FileTransferCommand(byte address, FileTransferFragment fileTransferFragment)
        {
            Address = address;
            _fileTransferFragment = fileTransferFragment;
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
            return _fileTransferFragment.BuildData().ToArray();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {

        }
    }
}