using System;
using System.Collections.Generic;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages
{
    public class EncryptionKeySetCommand : Command
    {
        private readonly EncryptionKeyConfiguration _encryptionKeyConfiguration;

        public EncryptionKeySetCommand(byte address, EncryptionKeyConfiguration encryptionKeyConfiguration)
        {
            Address = address;
            _encryptionKeyConfiguration = encryptionKeyConfiguration ??
                                          throw new ArgumentNullException(nameof(encryptionKeyConfiguration));
        }

        protected override byte CommandCode => 0x75;

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
            return _encryptionKeyConfiguration.BuildData();
        }

        protected override void CustomCommandUpdate(List<byte> commandBuffer)
        {
            
        }
    }
}