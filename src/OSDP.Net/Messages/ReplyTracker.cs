using System;
using OSDP.Net.Messages.ACU;

namespace OSDP.Net.Messages;

internal class ReplyTracker
{
    private Command _issuingCommand;
    private readonly Device _device;

    public ReplyTracker(Guid connectionId, IncomingMessage message, Command issuingCommand, Device device)
    {
        ConnectionId = connectionId;
        Message = message;
        _issuingCommand = issuingCommand;
        _device = device;
    }

    public IncomingMessage Message { get; }
    
    public Guid ConnectionId { get; }
    
    public bool IsValidReply => Message.Address == _issuingCommand.Address && Message.IsDataCorrect;

    public bool MatchIssuingCommand(Command command) => command.Equals(_issuingCommand);
    
    internal bool ValidateSecureChannelEstablishment()
    {
        if (!Message.SecureCryptogramHasBeenAccepted())
        {
            return false;
        }

        _device.MessageSecureChannel.Establish(Message.Payload);

        return true;
    }
}