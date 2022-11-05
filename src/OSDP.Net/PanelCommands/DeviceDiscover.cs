﻿
using OSDP.Net.Connections;
using OSDP.Net.Model.ReplyData;
using OSDP.Net.Tracing;
using System;
using System.Text;
using System.Threading;

namespace OSDP.Net.PanelCommands.DeviceDiscover
{
    /// <summary>
    /// This namespace contains type definitions used specifically for 
    /// <see cref="ControlPanel.DiscoverDevice(System.Collections.Generic.IEnumerable{IOsdpConnection}, DiscoveryOptions)"/>
    /// </summary>
    public static class NamespaceDoc { }

    /// <summary>
    /// Represents an error condition encountered during device discovery
    /// </summary>
    public class DeviceDiscoveryException : OSDPNetException
    {
        /// <summary>
        /// Initializes a new instance of DeviceDiscoveryException
        /// </summary>
        /// <param name="message"></param>
        public DeviceDiscoveryException(string message) : base(message) { }
    }

    /// <summary>
    /// Represents current state of the device discovery process. As the device discovery
    /// unfolds, this status value indicates which fields of the <see cref="DiscoveryResult"/>
    /// instance have been filled in.
    /// </summary>
    public enum DiscoveryStatus
    {
        /// <summary>
        /// Device discovery has been started
        /// </summary>
        Started,

        /// <summary>
        /// About to send an osdp_POLL to broadcast address (0x7f) to see if a device
        /// will respond on the connection being tested
        /// </summary>
        BroadcastOnConnection,

        /// <summary>
        /// Received a valid reply too osdp_POLL indicating that the right connection
        /// has been found
        /// </summary>
        ConnectionWithDeviceFound,

        /// <summary>
        /// About to send an osdp_PDID to a specific address to see if a device will
        /// respond and if it does to get its identification report
        /// </summary>
        LookingForDeviceAtAddress,

        /// <summary>
        /// Received a valid reply to osdp_PDID command, device has been found and
        /// its address determined
        /// </summary>
        DeviceIdentified,

        /// <summary>
        /// About to send an osdp_CAP command to query for device capabilities
        /// </summary>
        QueryingDeviceCapabilities,

        /// <summary>
        /// Received a valid reply to osdp_CAM command
        /// </summary>
        CapabilitiesDiscovered,

        /// <summary>
        /// Device discovery completed successfully
        /// </summary>
        Succeeded,

        /// <summary>
        /// No devices have been found
        /// </summary>
        DeviceNotFound,

        /// <summary>
        /// Something unexpected has gone wrong during the device discovery process
        /// </summary>
        Error,

        /// <summary>
        /// Device discovery process terminated due to a cancellation request
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// Callback used for reporting device discovery status updates. 
    /// </summary>
    /// <param name="current">Discover results instance. This object will get incrementally
    /// filled in as the device discovery progresses. Check status field to determine
    /// which fields have valid values in them.</param>
    public delegate void DiscoveryProgress(DiscoveryResult current);

    /// <summary>
    /// Defines a set of optional parameters the caller can pass in to the
    /// <see cref="ControlPanel.DiscoverDevice(System.Collections.Generic.IEnumerable{IOsdpConnection}, DiscoveryOptions)"/>
    /// call.
    /// </summary>
    public class DiscoveryOptions
    {
        /// <summary>
        /// Callback to invoke when there is a status update
        /// </summary>
        public DiscoveryProgress ProgressCallback { get; set; } = null;

        /// <summary>
        /// Maximum time to wait for a poll command to return. This is important here since if a
        /// connection doesn't have a device or a poll is sent to an unknown address, there will
        /// be no reply. Therefore, this timeout interval determines the maximum time of the
        /// device discovery
        /// </summary>
        public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Tracer instance to use to report network commands/replies as issued by the 
        /// device discovery
        /// </summary>
        public Action<TraceEntry> Tracer { get; set; } = null;

        /// <summary>
        /// Cancellation token to listen to if the caller wishes to be able to interrupt
        /// device discovery
        /// </summary>
        public CancellationToken CancellationToken { get; set; } = default;

        /// <summary>
        /// The library has a default tracer that will always write to [connectionId].osdpcap
        /// file in the current working directory of the calling process. Call this method to
        /// set <see cref="Tracer"/> property to this default instance.
        /// </summary>
        /// <param name="setDefault">Flag that indicates whether or not default tracer is to
        /// be used. From API perspective, this seems a bit silly to have, but its here
        /// because it made calling code slightly more conscise</param>
        /// <returns></returns>
        public DiscoveryOptions WithDefaultTracer(bool setDefault=true)
        {
            if (setDefault) Tracer = OSDPFileCapTracer.Trace;
            return this;
        }
    }

    /// <summary>
    /// Contains the results of the device discovery. When <see cref="DiscoveryOptions.ProgressCallback"/> is
    /// used, this instance will get incrementally filled in based on current value of the <see cref="Status"/> field
    /// </summary>
    public class DiscoveryResult
    {
        /// <summary>
        /// Determines current state of the device discovery
        /// </summary>
        public DiscoveryStatus Status { get; internal set; }

        /// <summary>
        /// When Status is <see cref="DiscoveryStatus.BroadcastOnConnection"/> this property identifies
        /// connection being tested even if ultimately no device will be discovered on it. After 
        /// <see cref="DiscoveryStatus.ConnectionWithDeviceFound"/> status, this property identifies
        /// connection with a valid device
        /// </summary>
        public IOsdpConnection Connection { get; internal set; }

        /// <summary>
        /// When Status is <see cref="DiscoveryStatus.LookingForDeviceAtAddress"/>, this property
        /// identifies the address being polled to see if there is an available device. After
        /// <see cref="DiscoveryStatus.DeviceIdentified"/> status, this property identifies the
        /// address where a device was found
        /// </summary>
        public byte Address { get; internal set; }

        /// <summary>
        /// After status <see cref="DiscoveryStatus.DeviceIdentified"/>, this property will contain
        /// the identification report from the device
        /// </summary>
        public DeviceIdentification Id { get; internal set; }

        /// <summary>
        /// After status <see cref="DiscoveryStatus.CapabilitiesDiscovered"/>, this property will contain
        /// the capabilities report from the device
        /// </summary>
        public DeviceCapabilities Capabilities { get; internal set; }

        /// <summary>
        /// When status is set to <see cref="DiscoveryStatus.Error"/> or <see cref="DiscoveryStatus.Cancelled"/>
        /// this property will be set to the exception instance that terminated the device discovery
        /// </summary>
        public Exception Error { get; internal set; }

        /// <inheritdoc/>
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