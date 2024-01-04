using System;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Messages.ACU
{
    internal class AuthenticationChallengeCommand : Command
    {
        private readonly MessageDataFragment _messageDataFragment;

        public AuthenticationChallengeCommand(byte address, MessageDataFragment messageDataFragment)
        {
            _messageDataFragment = messageDataFragment;
            Address = address;
        }

        protected override byte CommandCode => (byte)CommandType.AuthenticateChallenge;

        protected override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return SecurityBlock.CommandMessageWithDataSecurity;
        }

        protected override ReadOnlySpan<byte> Data()
        {
            return _messageDataFragment.BuildData();
        }

        protected override void CustomCommandUpdate(Span<byte> commandBuffer)
        {
        }
    }
}