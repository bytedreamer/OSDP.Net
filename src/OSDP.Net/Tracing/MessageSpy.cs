using System;
using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Tracing;

internal class MessageSpy
{
        private readonly SecurityContext _context;
        private readonly MessageSecureChannel _commandSpyChannel;
        private readonly MessageSecureChannel _replySpyChannel;

        public MessageSpy(byte[] securityKey = null)
        {
            _context = new SecurityContext(securityKey);
            _commandSpyChannel = new PdMessageSecureChannelBase(_context);
            _replySpyChannel = new ACUMessageSecureChannel(_context);
        }

    public byte PeekAddressByte(ReadOnlySpan<byte> data)
    {
        return data[1];
    }

    public IncomingMessage ParseCommand(byte[] data)
    {
        var command = new IncomingMessage(data, _commandSpyChannel);

        return (CommandType)command.Type switch
        {
            CommandType.SessionChallenge => HandleSessionChallenge(command),
            CommandType.ServerCryptogram => HandleSCrypt(command),
            _ => command
        };
    }

    public IncomingMessage ParseReply(byte[] data)
    {
        var reply = new IncomingMessage(data, _replySpyChannel);

        return (ReplyType)reply.Type switch
        {
            ReplyType.InitialRMac => HandleInitialRMac(reply),
            _ => reply
        };
    }

        private IncomingMessage HandleSessionChallenge(IncomingMessage command)
        {
            byte[] rndA = command.Payload;
            var crypto = _context.CreateCypher(true);
            _context.Enc = SecurityContext.GenerateKey(crypto, new byte[] { 0x01, 0x82, rndA[0], rndA[1], rndA[2], rndA[3], rndA[4], rndA[5] });
            _context.SMac1 = SecurityContext.GenerateKey(crypto, new byte[] { 0x01, 0x01, rndA[0], rndA[1], rndA[2], rndA[3], rndA[4], rndA[5] });
            _context.SMac2 = SecurityContext.GenerateKey(crypto, new byte[] { 0x01, 0x02, rndA[0], rndA[1], rndA[2], rndA[3], rndA[4], rndA[5] });
            return command;
        }

        private IncomingMessage HandleSCrypt(IncomingMessage command)
        {
            var serverCryptogram = command.Payload;
            using var crypto = _context.CreateCypher(true, _context.SMac1);
            var intermediate = SecurityContext.GenerateKey(crypto, serverCryptogram);
            crypto.Key = _context.SMac2;
            _context.RMac = SecurityContext.GenerateKey(crypto, intermediate);
            return command;
        }

    private IncomingMessage HandleInitialRMac(IncomingMessage reply)
    {
        _context.IsSecurityEstablished = true;
        return reply;
    }
}