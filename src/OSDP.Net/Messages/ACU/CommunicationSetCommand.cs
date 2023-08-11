using System;
using System.Linq;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages.ACU
{
    internal class CommunicationSetCommand : Command
    {
        private readonly CommunicationConfiguration _communicationConfiguration;

        public CommunicationSetCommand(byte address, CommunicationConfiguration communicationConfiguration)
        {
            _communicationConfiguration = communicationConfiguration;
            Address = address;
        }

        protected override byte CommandCode => (byte)CommandType.CommunicationSet;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithDataSecurity;
        }

        protected override ReadOnlySpan<byte> Data()
        {
            return _communicationConfiguration.BuildData().ToArray();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {

        }
    }
}