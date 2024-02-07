using System;
using OSDP.Net.Messages.ACU;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model;

namespace OSDP.Net.Messages;

internal class OutgoingMessage : Message
{
    private const int StartOfMessageLength = 5;
    private readonly PayloadData _data;

    internal OutgoingMessage(byte address, Control controlBlock, PayloadData data)
    {
        Address = address;
        ControlBlock = controlBlock;
        _data = data;
    }

    internal Control ControlBlock { get; }
    
    internal byte[] BuildMessage(IMessageSecureChannel secureChannel)
    {
        var payload = _data.BuildData();
        if (secureChannel.IsSecurityEstablished)
        {
            payload = PadTheData(payload, 16, FirstPaddingByte);
        }

        bool isSecurityBlockPresent = secureChannel.IsSecurityEstablished ||
                                      _data.MessageType == (byte)ReplyType.CrypticData ||
                                      _data.MessageType == (byte)ReplyType.InitialRMac;
        int headerLength = StartOfMessageLength + (isSecurityBlockPresent ? 3 : 0) + sizeof(ReplyType);
        int totalLength = headerLength + payload.Length +
                          (ControlBlock.UseCrc ? 2 : 1) +
                          (secureChannel.IsSecurityEstablished ? MacSize : 0);
        var buffer = new byte[totalLength];
        int currentLength = 0;

        buffer[currentLength++] = StartOfMessage;
        buffer[currentLength++] = Address;
        buffer[currentLength++] = (byte)(totalLength & 0xff);
        buffer[currentLength++] = (byte)((totalLength >> 8) & 0xff);
        buffer[currentLength++] = ControlBlock.ControlByte;

        if (isSecurityBlockPresent)
        {
            buffer[currentLength] = 0x03;
            buffer[currentLength + 1] = _data.MessageType == (byte)ReplyType.CrypticData
                ? (byte)SecurityBlockType.SecureConnectionSequenceStep2
                : _data.MessageType == (byte)ReplyType.InitialRMac
                    ? (byte)SecurityBlockType.SecureConnectionSequenceStep4
                    : payload.Length == 0
                        ? (byte)SecurityBlockType.ReplyMessageWithNoDataSecurity
                        : (byte)SecurityBlockType.ReplyMessageWithDataSecurity;

            // TODO: How do I determine this properly?? (SCBK vs SCBK-D value)
            // Is this needed only for establishing secure channel? or do we always need to return it
            // with every reply?
            buffer[currentLength + 2] = 0x01;
            currentLength += 3;
        }

        buffer[currentLength++] = _data.MessageType;

        if (secureChannel.IsSecurityEstablished)
        {
            secureChannel.EncodePayload(payload, buffer.AsSpan(currentLength));
            currentLength += payload.Length;
            secureChannel.GenerateMac(buffer.AsSpan(0, currentLength), false)
                .Slice(0, MacSize)
                .CopyTo(buffer.AsSpan(currentLength));
            currentLength += MacSize;
        }
        else
        {
            payload.CopyTo(buffer, currentLength);
            currentLength += payload.Length;
        }
        
        if (ControlBlock.UseCrc)
        {
            AddCrc(buffer);
            currentLength += 2;
        }
        else
        {
            AddChecksum(buffer);
            currentLength++;
        }

        if (currentLength != buffer.Length)
        {
            throw new Exception(
                $"Invalid processing of reply data, expected length {currentLength}, actual length {buffer.Length}");
        }

        // Section 5.7 states that transmitting device shall guarantee an idle time between packets. This is
        // accomplished by sending a character with all bits set to 1. The driver byte is required by
        // converters and multiplexers to sense when line is idle.
        var messageBuffer = new byte[buffer.Length + 1];
        messageBuffer[0] = Bus.DriverByte;
        Buffer.BlockCopy(buffer, 0, messageBuffer, 1, buffer.Length);

        return messageBuffer;
    }

    protected override ReadOnlySpan<byte> Data()
    {
        return _data.BuildData();
    }
}