using OSDP.Net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// The PD device capabilities data sent as a reply.
    /// </summary>
    public class DeviceCapabilities : PayloadData
    {
        /// <summary>
        /// Creates a new instance of DeviceCapabilities
        /// </summary>
        public DeviceCapabilities()
        {
        }

        /// <summary>
        /// Initializes a new instance of DeviceCapabilities class 
        /// </summary>
        /// <param name="capabilities">List of DeviceCapability instances</param>
        public DeviceCapabilities(IEnumerable<DeviceCapability> capabilities)
        {
            Capabilities = capabilities;
        }

        /// <summary>
        /// Gets the all the PD's device capabilities.
        /// </summary>
        public IEnumerable<DeviceCapability> Capabilities { get; private set; }
        
        /// <inheritdoc/>
        public override byte Code => (byte)ReplyType.PdCapabilitiesReport;

        /// <summary>
        /// Gets a specific PD capability
        /// </summary>
        /// <param name="funcCode">Function code of the capability to get</param>
        /// <returns>
        /// Either a <see cref="DeviceCapability"/> instance or null if one wasn't found for a given function code.
        /// </returns>
        public DeviceCapability Get(CapabilityFunction funcCode) =>
            Capabilities.FirstOrDefault((cap) => cap.Function == funcCode);

        /// <inheritdoc cref="DeviceCapabilities.Get(CapabilityFunction)"/>
        /// <summary>
        /// Strongly timed version of the more generic Get method
        /// </summary>
        public T Get<T>(CapabilityFunction funcCode) where T : DeviceCapability => (T)Get(funcCode);

        /// <summary>Parses the message payload bytes</summary>
        /// <param name="data">Message payload as bytes</param>
        /// <returns>An instance of DeviceCapabilities representing the message payload</returns>
        internal static DeviceCapabilities ParseData(ReadOnlySpan<byte> data)
        {
            var dataArray = data.ToArray();
            if (dataArray.Length % 3 != 0)
            {
                throw new Exception("Invalid size for the data");
            }

            var capabilities = new List<DeviceCapability>();
            for (int index = 0; index < dataArray.Length; index += 3)
            {
                capabilities.Add(DeviceCapability.ParseData(dataArray.Skip(index).Take(3).ToArray()));
            }

            var deviceCapabilities = new DeviceCapabilities
            {
                Capabilities = capabilities
            };

            return deviceCapabilities;
        }


        /// <inheritdoc />
        public override string ToString()
        {
            var build = new StringBuilder();
            foreach (var capability in Capabilities)
            {
                build.AppendLine(capability.ToString());
            }

            return build.ToString();
        }

        /// <inheritdoc/>
        public override byte[] BuildData()
        {
            var data = new List<byte>();
            foreach (var capability in Capabilities)
            {
                data.AddRange(capability.BuildData());
            }

            return data.ToArray();
        }
    }
}