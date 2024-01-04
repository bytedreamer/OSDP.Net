using OSDP.Net.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// Generic capability data for a PD
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

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"  Function: {Helpers.SplitCamelCase(Function.ToString())}");
            sb.AppendLine($"Compliance: {Compliance}");
            sb.AppendLine($" Number Of: {NumberOf}");
            return sb.ToString();
        }

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
                {CapabilityFunction.CommunicationSecurity, () => new CommSecurityDeviceCap() },
                {CapabilityFunction.ReceiveBufferSize, () => new RcvBuffSizeDeviceCap() },
                {CapabilityFunction.LargestCombinedMessageSize, () => new LargestCombMsgSizeDeviceCap() }
            };
    }

    /// <summary>
    /// Common base for the device capabilities that identify byte sizes
    /// </summary>
    public abstract class MsgSizeDeviceCap : DeviceCapability
    {
        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"  Function: {Helpers.SplitCamelCase(Function.ToString())}");
            sb.AppendLine($"      Size: {Message.ConvertBytesToUnsignedShort(new[] { Compliance, NumberOf })}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Receive Buffer Size PD capability
    /// </summary>
    public class RcvBuffSizeDeviceCap : MsgSizeDeviceCap;

    /// <summary>
    /// Largest Combined Message Size PD capability
    /// </summary>
    public class LargestCombMsgSizeDeviceCap : MsgSizeDeviceCap;

    /// <summary>
    /// Communication Security PD capability
    /// </summary>
    public class CommSecurityDeviceCap : DeviceCapability
    {
        /// <summary>
        /// Flag indicating whether or not PD support AES-128
        /// </summary>
        public bool SupportsAes128 { get => (Compliance & 0x01) != 0; }

        /// <summary>
        /// Flag indicating whether or not PD is using a default encryption key
        /// </summary>
        public bool UsesDefaultKey { get => (NumberOf & 0x01) != 0; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"  Function: {Helpers.SplitCamelCase(Function.ToString())}");
            sb.AppendLine($"Supports AES-128: {SupportsAes128}");
            sb.AppendLine($"Uses Default Key: {UsesDefaultKey}");
            return sb.ToString();
        }
    }
}
