
using OSDP.Net.Connections;
using OSDP.Net.Model.ReplyData;
using System;
using System.Text;

namespace OSDP.Net.PanelCommands.DeviceDiscover
{
    public class DeviceDiscoveryException : OSDPNetException
    {
        public DeviceDiscoveryException(string message) : base(message) { }
    }

    public class DiscoveryResult
    {
        public IOsdpConnection Connection { get; internal set; }

        public byte Address { get; internal set; }

        public DeviceIdentification Id { get; internal set; }
        
        public DeviceCapabilities Capabilities { get; internal set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"    Baud Rate: {Connection.BaudRate}");
            sb.AppendLine($"      Address: {Address}");
            sb.AppendLine("Identification:");
            sb.Append("        ");
            sb.AppendLine(Id.ToString().Trim().Replace("\n", "\n        "));
            sb.AppendLine("  Capabilities:");
            sb.Append("        ");
            sb.AppendLine(Capabilities.ToString().Trim().Replace("\n", "\n        "));

            return sb.ToString();
        }
    }
}
