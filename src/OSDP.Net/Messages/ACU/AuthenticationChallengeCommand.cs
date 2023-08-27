using System;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages.ACU
{
    internal class AuthenticationChallengeCommand : Command
    {
        private readonly AuthenticationChallengeFragment _authenticationChallengeFragment;

        public AuthenticationChallengeCommand(byte address, AuthenticationChallengeFragment authenticationChallengeFragment)
        {
            _authenticationChallengeFragment = authenticationChallengeFragment;
            Address = address;
        }

        protected override byte CommandCode => (byte)CommandType.AuthenticateChallenge;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithDataSecurity;
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