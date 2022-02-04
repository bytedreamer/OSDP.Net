using System;

namespace OSDP.Net.Tracing;

/// <summary>
/// Direction of trace.
/// </summary>
public enum TraceDirection
{
    /// <summary>
    /// Data is received by the ControlPanel.
    /// </summary>
    In,
    /// <summary>
    /// Dta is sent from the ControlPanel.
    /// </summary>
    Out,
}

/// <summary>
/// Represents low level data sent on the wire between the control panel and the devices.
/// </summary>
public struct TraceEntry
{
    /// <summary>
    /// The direction in which the data is sent.
    /// </summary>
    public TraceDirection Direction { get; private set; }

    /// <summary>
    /// The connection sending/receiving the data.
    /// </summary>
    public Guid ConnectionId { get; private set; }

    /// <summary>
    /// The data that is sent/received
    /// </summary>
    public byte[] Data { get; private set; }

    internal TraceEntry(TraceDirection direction, Guid connectionId, byte[] data)
    {
        Direction = direction;
        ConnectionId = connectionId;
        Data = data;
    }
}
