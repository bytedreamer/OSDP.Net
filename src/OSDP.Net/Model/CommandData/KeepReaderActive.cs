using System;
using System.Linq;
using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Model.CommandData;

/// <summary>
/// Command data to set the maximum message size the ACU can receive.
/// </summary>
public class KeepReaderActive : CommandData
{
    public KeepReaderActive(ushort keepAliveTimeInMilliseconds)
    {
        KeepAliveTimeInMilliseconds = keepAliveTimeInMilliseconds;
    }

    public ushort KeepAliveTimeInMilliseconds { get; }
    
    /// <inheritdoc />
    public override CommandType CommandType => CommandType.MaxReplySize;

    /// <inheritdoc />
    public override byte Code => (byte)CommandType;
        
    /// <inheritdoc />
    public override ReadOnlySpan<byte> SecurityControlBlock() => SecurityBlock.CommandMessageWithDataSecurity;

    /// <inheritdoc />
    public override void CustomMessageUpdate(Span<byte> messageBuffer)
    {
    }

    /// <inheritdoc />
    public override byte[] BuildData()
    {
        return Message.ConvertShortToBytes(KeepAliveTimeInMilliseconds);
    }

    /// <summary>Parses the message payload bytes</summary>
    /// <param name="data">Message payload as bytes</param>
    /// <returns>An instance of KeepReaderActive representing the message payload</returns>
    public static KeepReaderActive ParseData(ReadOnlySpan<byte> data)
    {
        return new KeepReaderActive(Message.ConvertBytesToUnsignedShort(data));
    }
}