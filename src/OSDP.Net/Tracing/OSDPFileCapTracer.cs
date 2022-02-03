namespace OSDP.Net.Tracing;

internal class OSDPFileCapTracer : ITracer
{
    public void Dispose()
    {
        // TODO: Dispose file
    }

    public void Trace(OSDPTraceEntry trace)
    {
        // TODO: Create file if not done.
        // Serialize in OSDPCap format to file
    }
}
