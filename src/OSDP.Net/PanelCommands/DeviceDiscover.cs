
using OSDP.Net.Connections;
using OSDP.Net.Model.ReplyData;
using OSDP.Net.Tracing;
using System;
using System.Text;
using System.Threading;

namespace OSDP.Net.PanelCommands.DeviceDiscover
{
    public class DeviceDiscoveryException : OSDPNetException
    {
        public DeviceDiscoveryException(string message) : base(message) { }
    }

    public enum DiscoveryStatus
    {
        Started,
        BroadcastOnConnection,
        ConnectionWithDeviceFound,
        LookingForDeviceAtAddress,
        DeviceIdentified,
        QueryingDeviceCapabilities,
        CapabilitiesDiscovered,
        Succeeded,
        DeviceNotFound,
        Error,
        Cancelled
    }

    public delegate void DiscoveryProgress(DiscoveryResult current);

    public class DiscoveryOptions
    {
        public DiscoveryProgress ProgressCallback { get; set; } = null;

        public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromMilliseconds(500);

        public Action<TraceEntry> Tracer { get; set; } = null;

        public CancellationToken CancellationToken { get; set; } = default;

        public DiscoveryOptions WithDefaultTracer(bool setDefault=true)
        {
            if (setDefault) Tracer = OSDPFileCapTracer.Trace;
            return this;
        }
    }

    public class DiscoveryResult
    {
        public DiscoveryStatus Status { get; internal set; }

        public IOsdpConnection Connection { get; internal set; }

        public byte Address { get; internal set; }

        public DeviceIdentification Id { get; internal set; }
        
        public DeviceCapabilities Capabilities { get; internal set; }

        public Exception Error { get; internal set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"    Baud Rate: {Connection.BaudRate}");
            sb.AppendLine($"      Address: {Address}");
            sb.AppendLine("Identification:");
            sb.Append("        ");
            sb.AppendLine(Id.ToString().TrimEnd().Replace("\n", "\n        "));
            sb.AppendLine("  Capabilities:");
            sb.Append("        ");
            sb.AppendLine(Capabilities.ToString().TrimEnd().Replace("\n", "\n        "));

            return sb.ToString();
        }
    }
}
