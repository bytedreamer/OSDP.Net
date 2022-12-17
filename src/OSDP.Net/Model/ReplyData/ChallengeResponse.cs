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
    /// Represents a reply to osdp_CHLNG command and contain's PD's ID, 
    /// PD-generated random number and PD cryptogram
    /// </summary>
    public class ChallengeResponse : ReplyData
    {
        private byte[] _payload;

        /// <summary>
        /// Create a new instance of the ChallengeResponse
        /// </summary>
        /// <param name="cUID">PD(Client) ID to send back to ACU</param>
        /// <param name="rndB">PD random number</param>
        /// <param name="cryptogram">PD Cryptogram</param>
        public ChallengeResponse(byte[] cUID, byte[] rndB, byte[] cryptogram)
        {
            _payload = new byte[cUID.Length + rndB.Length + cryptogram.Length];
            cUID.CopyTo(_payload, 0);
            rndB.CopyTo(_payload, cUID.Length);
            cryptogram.CopyTo(_payload, cUID.Length + rndB.Length);
        }

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

            return _payload;
        }

        /// <inheritdoc/>
        public override ReplyType ReplyType => ReplyType.CrypticData;
    }
}
