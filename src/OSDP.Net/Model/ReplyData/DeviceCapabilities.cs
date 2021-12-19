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
                build.AppendLine($"  Function: {Message.SplitCamelCase(capability.Function.ToString())}");

                if (capability.Function is CapabilityFunction.ReceiveBufferSize
                    or CapabilityFunction.LargestCombinedMessageSize)
                {
                    build.AppendLine(
                        $"      Size: {Message.ConvertBytesToUnsignedShort(new[] { capability.Compliance, capability.NumberOf })}");
                }
                else
                {
                    build.AppendLine($"Compliance: {capability.Compliance}");
                    build.AppendLine($" Number Of: {capability.NumberOf}");
                }

                build.AppendLine(string.Empty);
            }

            return build.ToString();
        }
    }
}