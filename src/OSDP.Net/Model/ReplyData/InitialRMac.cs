using System;
using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// Represents the payload of osdp_RMAC_I reply
    /// </summary>
    public class InitialRMac : PayloadData
    {
        /// <summary>
        /// Creates a new instance of InitialRMac
        /// </summary>
        /// <param name="rmac"></param>
        public InitialRMac(byte[] rmac, bool isDefaultKey)
        {
            RMac = rmac;
            IsDefaultKey = isDefaultKey;
        }

        /// <summary>
        /// Initial R-MAC to be used as a seed for MAC signing of the next
        /// command message
        /// </summary>
        public byte[] RMac { get; }
        
        public bool IsDefaultKey { get; }

        /// <inheritdoc/>
        public override byte Code => (byte)ReplyType.InitialRMac;
        
        /// <inheritdoc />
        public override bool IsSecurityInitialization => true;
        
        /// <inheritdoc />
        public override ReadOnlySpan<byte> SecurityControlBlock()
        {
            return new byte[]
            {
                0x03,
                (byte)SecurityBlockType.SecureConnectionSequenceStep2,
                (byte)(IsDefaultKey ? 0x00 : 0x01)
            };
        }

        /// <inheritdoc/>
        public override byte[] BuildData()
        {
            return RMac;
        }
    }
}
