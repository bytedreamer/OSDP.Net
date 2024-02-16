using System;
using OSDP.Net.Messages;

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
        public InitialRMac(byte[] rmac)
        {
            RMac = rmac;
        }

        /// <summary>
        /// Initial R-MAC to be used as a seed for MAC signing of the next
        /// command message
        /// </summary>
        public byte[] RMac { get; }

        /// <inheritdoc/>
        public override byte Code => (byte)ReplyType.InitialRMac;
        
        /// <inheritdoc />
        public override bool IsSecurityInitialization => true;

        /// <inheritdoc/>
        public override byte[] BuildData()
        {
            return RMac;
        }
    }
}
