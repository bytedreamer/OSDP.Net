using System;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages
{
    internal class AuthenticationChallengeCommand : Command
    {
        private readonly AuthenticationChallengeFragment _authenticationChallengeFragment;

        public AuthenticationChallengeCommand(byte address, AuthenticationChallengeFragment authenticationChallengeFragment)
        {
            _authenticationChallengeFragment = authenticationChallengeFragment;
            Address = address;
        }

        protected override byte CommandCode => 0xA5;

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
            return _authenticationChallengeFragment.BuildData();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }
    }
}