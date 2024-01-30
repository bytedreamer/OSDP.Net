using System;
using OSDP.Net.Messages.ACU;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model;

namespace OSDP.Net.Messages;

public class OutgoingMessage : Message
{
    private const int StartOfMessageLength = 5;
    private readonly PayloadData _data;

    public OutgoingMessage(Control controlBlock, PayloadData data)
    {
        ControlBlock = controlBlock;
        _data = data;
    }

    public Control ControlBlock { get; }

    internal byte[] BuildMessage(IMessageSecureChannel secureChannel, byte[] prefix = default)
    {
        var payload = _data.BuildData();
        if (secureChannel.IsSecurityEstablished)
        {
            payload = PadTheData(payload, 16, FirstPaddingByte);
        }

        bool isSecurityBlockPresent = secureChannel.IsSecurityEstablished ||
                                      _data.Type == (byte)ReplyType.CrypticData ||
                                      _data.Type == (byte)ReplyType.InitialRMac;
        int headerLength = StartOfMessageLength + (isSecurityBlockPresent ? 3 : 0) + sizeof(ReplyType);
        int totalLength = headerLength + payload.Length +
                          (ControlBlock.UseCrc ? 2 : 1) +
                          (secureChannel.IsSecurityEstablished ? MacSize : 0);
        var buffer = new byte[totalLength];
        int currentLength = 0;

        if (prefix != null)
        {
            prefix.CopyTo(buffer, currentLength);
            currentLength += prefix.Length;
        }

        buffer[currentLength++] = StartOfMessage;
        buffer[currentLength++] = Address;
        buffer[currentLength++] = (byte)(totalLength & 0xff);
        buffer[currentLength++] = (byte)((totalLength >> 8) & 0xff);
        buffer[currentLength++] = ControlBlock.ControlByte;

        if (isSecurityBlockPresent)
        {
            buffer[currentLength] = 0x03;
            buffer[currentLength + 1] = _data.Type == (byte)ReplyType.CrypticData
                ? (byte)SecurityBlockType.SecureConnectionSequenceStep2
                : _data.Type == (byte)ReplyType.InitialRMac
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

        buffer[currentLength] = _data.Type;
        currentLength++;

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

        // TODO: decide on CRC vs Checksum based on incoming command and do the same.
        // Is this a valid assumption??
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

        return buffer;
    }

    protected override ReadOnlySpan<byte> Data()
    {
        return _data.BuildData();
    }
}