using System;
using System.Linq;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// Individual capability data for a PD
    /// </summary>
    public class DeviceCapability
    {
        private DeviceCapability()
        {
        }

        /// <summary>
        /// Gets the function code.
        /// </summary>
        public CapabilityFunction Function { get; private set; }

        /// <summary>
        /// Gets level of compliance with defined function.
        /// </summary>
        public byte Compliance { get; private set; }

        /// <summary>
        /// Gets number of objects of this type.
        /// </summary>
        public byte NumberOf { get; private set; }

        internal static DeviceCapability ParseData(byte[] data)
        {
            return new DeviceCapability
            {
                Function = typeof(CapabilityFunction).IsEnumDefined((int)data[0])
                    ? (CapabilityFunction)data[0]
                    : CapabilityFunction.Unknown,
                Compliance = data[1],
                NumberOf = data[2]
            };
        }
    }
}