using System;
using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Model.CommandData;

/// <summary>
/// Represents a command data object that doesn't have any payload data.
/// </summary>
internal class NoPayloadCommandData : CommandData
{
    public NoPayloadCommandData(CommandType commandType)
    {
        CommandType = commandType;
    }
    
    /// <inheritdoc />
    public override byte MessageType => (byte)CommandType;
    
    /// <inheritdoc />
    public override CommandType CommandType { get; }
    
    /// <inheritdoc />
    internal override ReadOnlySpan<byte> SecurityControlBlock() => SecurityBlock.CommandMessageWithNoDataSecurity;
    
    /// <inheritdoc />
    public override byte[] BuildData()
    {
        return Array.Empty<byte>();
    }
}