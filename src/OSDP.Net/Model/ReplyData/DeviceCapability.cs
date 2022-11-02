using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// Individual capability data for a PD
    /// </summary>
    public class DeviceCapability
    {
        private protected DeviceCapability()
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
            var func = typeof(CapabilityFunction).IsEnumDefined((int)data[0]) 
                ? (CapabilityFunction)data[0]
                : CapabilityFunction.Unknown;

            if (!_capFactories.TryGetValue(func, out var creator))
            {
                creator = () => new DeviceCapability();
            }

            var cap = creator();
            cap.Function = func;
            cap.Compliance = data[1];
            cap.NumberOf = data[2];
            return cap;
        }

        private static Dictionary<CapabilityFunction, Func<DeviceCapability>> _capFactories = new ()
            {
                {CapabilityFunction.CommunicationSecurity, () => new CommSecurityDeviceCap() }
            };
    }

    public class CommSecurityDeviceCap : DeviceCapability
    {
        internal CommSecurityDeviceCap() { }

        public bool SupportsAES128 { get => (Compliance & 0x01) != 0; }

        public bool UsesDefaultKey { get => (NumberOf & 0x01) != 0; }
    }
}
