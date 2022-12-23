using System;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages.ACU
{
    internal class EncryptionKeySetCommand : Command
    {
        private readonly EncryptionKeyConfiguration _encryptionKeyConfiguration;

        public EncryptionKeySetCommand(byte address, EncryptionKeyConfiguration encryptionKeyConfiguration)
        {
            Address = address;
            _encryptionKeyConfiguration = encryptionKeyConfiguration ??
                                          throw new ArgumentNullException(nameof(encryptionKeyConfiguration));
        }

        protected override byte CommandCode => (byte)CommandType.KeySet;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithDataSecurity;
        }

        protected override ReadOnlySpan<byte> Data()
        {
            return _encryptionKeyConfiguration.BuildData();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }
    }
}