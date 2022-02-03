using System;

namespace OSDP.Net.Tracing;

public enum TraceDirection
{
    In,
    Out,
}

public class OSDPTraceEntry
{
    public TraceDirection Direction { get; set; }
    public Guid ConnectionId { get; set; }
    public byte[] Data { get; set; }
}


public interface ITracer : IDisposable
{
    void Trace(OSDPTraceEntry trace);
}
