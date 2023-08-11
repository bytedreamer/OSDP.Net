using System;
using System.Linq;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages.ACU
{
    internal class BiometricMatchCommand : Command
    {
        private readonly BiometricTemplateData _biometricTemplateData;

        public BiometricMatchCommand(byte address, BiometricTemplateData biometricTemplateData)
        {
            Address = address;
            _biometricTemplateData = biometricTemplateData;
        }

        protected override byte CommandCode => (byte)CommandType.BioMatch;

        protected override ReadOnlySpan<byte> Data()
        {
            return _biometricTemplateData.BuildData().ToArray();
        }

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithDataSecurity;
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }
    }
}