using System;
using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Model.CommandData;

internal class IdReport : CommandData
{
    public IdReport()
    {
    }
    
    /// <inheritdoc />
    public override CommandType CommandType => CommandType.IdReport;

    /// <inheritdoc />
    public override byte MessageType => (byte)CommandType;
    
    /// <inheritdoc />
    internal override ReadOnlySpan<byte> SecurityControlBlock() => SecurityBlock.CommandMessageWithDataSecurity;
    
    /// <inheritdoc />
    public override byte[] BuildData()
    {
        return new byte[] {0x00};
    }
}