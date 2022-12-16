using OSDP.Net.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSDP.Net.Model.ReplyData
{
    public class InitialRMac : ReplyData
    {
        public InitialRMac(byte[] rmac)
        {
            RMac = rmac;
        }

        public byte[] RMac { get; }

        public override ReplyType ReplyType => ReplyType.InitialRMac;

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
