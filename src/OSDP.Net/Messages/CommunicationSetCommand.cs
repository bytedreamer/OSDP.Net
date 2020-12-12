using System;
using System.Linq;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages
{
    public class CommunicationSetCommand : Command
    {
        private readonly CommunicationConfiguration _communicationConfiguration;

        public CommunicationSetCommand(byte address, CommunicationConfiguration communicationConfiguration)
        {
            _communicationConfiguration = communicationConfiguration;
            Address = address;
        }

        protected override byte CommandCode => 0x6E;

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
            return _communicationConfiguration.BuildData().ToArray();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
            
        }
    }
}