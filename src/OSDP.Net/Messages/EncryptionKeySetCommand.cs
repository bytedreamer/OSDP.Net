using System;
using System.Linq;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages
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

        protected override byte CommandCode => 0x75;

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
            return _encryptionKeyConfiguration.BuildData().ToArray();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }
    }
}