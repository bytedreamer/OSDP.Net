using System;
using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Model.CommandData;

internal class AuthenticationChallengeFragment : CommandData
{
    public AuthenticationChallengeFragment(MessageDataFragment fragment)
    {
        Fragment = fragment;
    }

    public MessageDataFragment Fragment { get; }
    
    /// <inheritdoc />
    public override CommandType CommandType => CommandType.AuthenticateChallenge;

    /// <inheritdoc />
    public override byte Code => (byte)CommandType;
        
    /// <inheritdoc />
    internal override ReadOnlySpan<byte> SecurityControlBlock() => SecurityBlock.CommandMessageWithDataSecurity;

    /// <inheritdoc />
    public override byte[] BuildData()
    {
        return Fragment.DataFragment;
    }

    /// <summary>Parses the message payload bytes</summary>
    /// <param name="data">Message payload as bytes</param>
    /// <returns>An instance of ACUReceivedSize representing the message payload</returns>
    public static AuthenticationChallengeFragment ParseData(ReadOnlySpan<byte> data)
    {
        return new AuthenticationChallengeFragment(MessageDataFragment.ParseData(data));
    }
}