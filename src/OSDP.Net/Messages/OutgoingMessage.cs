using System;

namespace OSDP.Net.Messages;

internal class OutgoingMessage : Message
{
    protected override ReadOnlySpan<byte> Data()
    {
        return Array.Empty<byte>();
    }
}