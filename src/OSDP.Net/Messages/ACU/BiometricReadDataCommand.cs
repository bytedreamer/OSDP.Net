using System;
using System.Linq;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages.ACU
{
    internal class BiometricReadDataCommand : Command
    {
        private readonly BiometricReadData _biometricReadData;

        public BiometricReadDataCommand(byte address, BiometricReadData biometricReadData)
        {
            Address = address;
            _biometricReadData = biometricReadData;
        }

        protected override byte CommandCode => (byte)CommandType.BioRead;

        protected override ReadOnlySpan<byte> Data()
        {
            return _biometricReadData.BuildData().ToArray();
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
