using OSDP.Net.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// Represents the payload of osdp_RMAC_I reply
    /// </summary>
    public class InitialRMac : ReplyData
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
        public override ReplyType ReplyType => ReplyType.InitialRMac;

        /// <inheritdoc/>
        public override byte[] BuildData(bool withPadding)
        {
            if (withPadding)
            {
                // This response is sent IN ORDER to establish
                // security. Therefore padding for AES should never be
                // applied to it
                throw new InvalidOperationException("Challenge response should never be padded!");
            }

            return RMac;
        }
    }
}
