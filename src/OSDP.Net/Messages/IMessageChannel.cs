using OSDP.Net.Model.ReplyData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSDP.Net.Messages
{
    public interface IMessageChannel
    {
        bool IsSecurityEstablished { get; }

        int PackPayload(byte[] payload, Span<byte> destination);

        ReadOnlySpan<byte> GenerateMac(ReadOnlySpan<byte> message, bool isCommand);
    }
}
