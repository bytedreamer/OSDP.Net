using System;

namespace OSDP.Net.Messages;

internal class ReplyTracker
{
    private readonly OutgoingMessage _issuingCommand;
    private readonly DeviceProxy _device;

    public ReplyTracker(Guid connectionId, IncomingMessage replyMessage, OutgoingMessage issuingCommand, DeviceProxy device)
    {
        ConnectionId = connectionId;
        ReplyMessage = replyMessage;
        _issuingCommand = issuingCommand;
        _device = device;
    }

    public IncomingMessage ReplyMessage { get; }
    
    public Guid ConnectionId { get; }
    
    public bool IsValidReply => ReplyMessage.Address == _issuingCommand.Address && ReplyMessage.IsDataCorrect;
    
    public bool MatchIssuingCommand(byte commandCode) => commandCode.Equals(_issuingCommand.Code);
    
    internal bool ValidateSecureChannelEstablishment()
    {
        if (!ReplyMessage.SecureCryptogramHasBeenAccepted())
        {
            return false;
        }

        _device.MessageSecureChannel.Establish(ReplyMessage.Payload);

        return true;
    }
}