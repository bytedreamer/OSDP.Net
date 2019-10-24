using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    public class DeviceCapabilities
    {
        private DeviceCapabilities()
        {
        }

        public IEnumerable<DeviceCapability> Capabilities { get; private set; }

        internal static DeviceCapabilities ParseData(Reply reply)
        {
            var data = reply.ExtractReplyData.ToArray();
            if (data.Length % 3 != 0)
            {
                throw new Exception("Invalid size for the data");
            }

            var capabilities = new List<DeviceCapability>();
            for (int index = 0; index < data.Length; index += 3)
            {
                capabilities.Add(DeviceCapability.ParseData(data.Skip(index).Take(3).ToArray()));
            }

            var deviceCapabilities = new DeviceCapabilities
            {
                Capabilities = capabilities
            };

            return deviceCapabilities;
        }

        public override string ToString()
        {
            var build = new StringBuilder();
            foreach (var capability in Capabilities)
            {
                build.AppendLine($"  Function: {Message.SplitCamelCase(capability.Function.ToString())}");

                if (capability.Function == CapabilityFunction.ReceiveBufferSize ||
                    capability.Function == CapabilityFunction.LargestCombinedMessageSize)
                {
                    build.AppendLine($"      Size: {Message.ConvertBytesToShort(new[] {capability.Compliance, capability.NumberOf})}");
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
    
    public class DeviceCapability
    {
        private DeviceCapability()
        {
        }

        public CapabilityFunction Function { get; private set; }
        public byte Compliance { get; private set; }
        public byte NumberOf { get; private set; }

        internal static DeviceCapability ParseData(byte[] data)
        {
            return new DeviceCapability
            {
                Function = data[0] <= 14 ? (CapabilityFunction) data[0] : CapabilityFunction.Unknown,
                Compliance = data[1],
                NumberOf = data[2]
            };
        }
    }
    
    public enum CapabilityFunction
    {
        Unknown = 0,
        ContactStatusMonitoring = 1,
        OutputControl = 2,
        CardDataFormat = 3,
        ReaderLEDControl = 4,
        ReaderAudibleOutput = 5,
        ReaderTextOutput = 6,
        TimeKeeping = 7,
        CheckCharacterSupport = 8,
        CommunicationSecurity = 9,
        ReceiveBufferSize = 10,
        LargestCombinedMessageSize = 11,
        SmartCardSupport = 12,
        Readers = 13,
        Biometrics = 14
    }
}