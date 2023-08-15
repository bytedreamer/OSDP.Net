using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;

using OSDP.Net.Messages;
using OSDP.Net.Model;
using OSDP.Net.Utilities;

namespace OSDP.Net.Tracing;

/// <summary>
/// Methods t decode packets that are recorded from a tracer
/// </summary>
public static class PacketDecoding
{
    /// <summary>
    /// Parse a raw message
    /// </summary>
    /// <param name="data">The byte data of the raw message starting with the SOM byte</param>
    /// <returns>The parse data of a packet</returns>
    public static Packet ParseMessage(ReadOnlySpan<byte> data)
    {
        var message = new IncomingMessage(data, null, Guid.Empty);
        return new Packet(message);
    }

    public static IEnumerable<OSDPCapEntry> OSDPCapParser(string json)
    {
        var lines = json.Split('\n');
        foreach (var line in lines)
        {
            dynamic entry = JsonSerializer.Deserialize<ExpandoObject>(line);

            DateTime dateTime = new DateTime(1970, 1, 1).AddSeconds(Double.Parse(entry.timeSec.ToString()))
                .AddTicks(long.Parse(entry.timeNano.ToString()) / 100L);
            TraceDirection.TryParse(entry.io.ToString(), true, out TraceDirection io);
            string data = entry.data.ToString();

            yield return new OSDPCapEntry(
                dateTime,
                io,
                ParseMessage(BinaryUtils.HexToBytes(data).ToArray()),
                entry.osdpTraceVersion.ToString(),
                entry.osdpSource.ToString());
        }
    }
}