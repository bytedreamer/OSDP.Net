using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// The PD device capabilities data sent as a reply.
    /// </summary>
    public class DeviceCapabilities
    {
        private DeviceCapabilities()
        {
        }

        /// <summary>
        /// Gets the all the PD's device capabilities.
        /// </summary>
        public IEnumerable<DeviceCapability> Capabilities { get; private set; }

        public DeviceCapability Get(CapabilityFunction funcCode) =>
            Capabilities.FirstOrDefault((cap) => cap.Function == funcCode);

        public T Get<T>(CapabilityFunction funcCode) where T : DeviceCapability => (T)Get(funcCode);

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
                build.Append(capability.ToString());
                build.AppendLine(string.Empty);
            }

            return build.ToString();
        }
    }
}