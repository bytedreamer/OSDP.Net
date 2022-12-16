using System;
using System.Linq;
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

        protected override byte CommandCode => 0x74;

        protected override ReadOnlySpan<byte> Data()
        {
            return _biometricTemplateData.BuildData().ToArray();
        }

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x02,
                0x17
            };
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }
    }
}