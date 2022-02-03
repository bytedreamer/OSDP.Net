using System;

namespace OSDP.Net.Tracing;

public enum TraceDirection
{
    In,
    Out,
}

public struct TraceEntry
{
    public TraceDirection Direction { get; set; }
    public Guid ConnectionId { get; set; }
    public byte[] Data { get; set; }

    public TraceEntry(TraceDirection direction, Guid connectionId, byte[] data)
    {
        Direction = direction;
        ConnectionId = connectionId;
        Data = data;
    }
}


public interface ITracer : IDisposable
{
    void Trace(TraceEntry trace);
}
