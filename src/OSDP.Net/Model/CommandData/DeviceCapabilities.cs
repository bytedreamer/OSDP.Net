using System;
using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Model.CommandData;

internal class DeviceCapabilities : CommandData
{
    public DeviceCapabilities()
    {
    }
    
    /// <inheritdoc />
    public override CommandType CommandType => CommandType.DeviceCapabilities;

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