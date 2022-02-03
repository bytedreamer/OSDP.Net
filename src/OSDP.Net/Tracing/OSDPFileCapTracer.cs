using System;
using System.IO;
using System.Text.Json;

namespace OSDP.Net.Tracing;

internal class OSDPFileCapTracer : ITracer
{
    private FileStream _tracerFile = null;

    public void Dispose()
    {
        _tracerFile?.Dispose();
    }

    public void Trace(TraceEntry trace)
    {
        if(_tracerFile == null)
        { 
            _tracerFile = new FileStream($"{trace.ConnectionId:D}.osdpcap", FileMode.OpenOrCreate);
        }

        // Serialize in OSDPCap format to file

        // TODO. How to handle this?
        //if (_tracerFile.CanWrite)
        //{
        //    _tracerFile = new FileStream($"{Id:D}.osdpcap", FileMode.OpenOrCreate);
        //}

        var unixTime = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1));
        long timeNano = (unixTime.Ticks - (long)Math.Floor(unixTime.TotalSeconds) * TimeSpan.TicksPerSecond) * 100L;
        JsonSerializer.Serialize(_tracerFile, new 
        {
            timeSec = Math.Floor(unixTime.TotalSeconds).ToString("F0"),
            timeNano = timeNano.ToString("000000000"),
            io = trace.Direction == TraceDirection.In ? "input" : "output",
            data = BitConverter.ToString(trace.Data),
            osdpTraceVersion = "1",
            osdpSource = "OSDP.Net"
        });

        // Write newline
        _tracerFile.WriteByte(0x0A);
        _tracerFile.Flush();
    }
}
