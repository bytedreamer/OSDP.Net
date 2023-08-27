using System;
using System.IO;
using System.Text.Json;

namespace OSDP.Net.Tracing;

internal static class OSDPFileCapTracer
{
    public static void Trace(TraceEntry trace)
    {
        var unixTime = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1));
        long timeNano = (unixTime.Ticks - (long)Math.Floor(unixTime.TotalSeconds) * TimeSpan.TicksPerSecond) * 100L;
        var line = JsonSerializer.Serialize(new 
        {
            timeSec = Math.Floor(unixTime.TotalSeconds).ToString("F0"),
            timeNano = timeNano.ToString("000000000"),
            io = trace.Direction == TraceDirection.Input ? "input" : "output",
            data = BitConverter.ToString(trace.Data),
            osdpTraceVersion = "1",
            osdpSource = "OSDP.Net"
        });
        File.AppendAllText($"{trace.ConnectionId:D}.osdpcap", line + "\n");
    }
}
