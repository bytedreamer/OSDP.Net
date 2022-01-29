using System;
using System.Linq;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages
{
    internal class BiometricReadDataCommand : Command
    {
        private readonly BiometricReadData _biometricReadData;

        public BiometricReadDataCommand(byte address, BiometricReadData biometricReadData)
        {
            Address = address;
            _biometricReadData = biometricReadData;
        }

        protected override byte CommandCode => 0x73;

        protected override ReadOnlySpan<byte> Data()
        {
            return _biometricReadData.BuildData().ToArray();
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