using OSDP.Net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// Base class representing a paylod of a PD reply message
    /// </summary>
    public abstract class ReplyData
    {
        /// <summary>
        /// Converts the reply into a byte array to be sent to ACU over the wire. The design
        /// decision to put the burden of adding padding on every deriving class is intentional
        /// as this method is where byte[] array originates and creating a payload that is
        /// ready to be encoded, helps us avoid a few downstream heap operations.
        /// </summary>
        /// <param name="withPadding">Indicates if returned packed data should be padded
        /// to a 16-byte boundary such that it is ready to be encrypted</param>
        /// <returns>Packed reply as array of raw bytes. Note that some types of replies,
        /// like osdp_ACK do not have additional data, in which case it is perfectly
        /// acceptable for this array to be 0 length</returns>
        public abstract byte[] BuildData(bool withPadding = false);

        /// <summary>
        /// Message reply code
        /// </summary>
        public abstract ReplyType ReplyType { get; }
    }
}
