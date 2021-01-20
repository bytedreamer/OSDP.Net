using System;
using System.Text;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// The local status report sent as a reply.
    /// </summary>
    public class LocalStatus
    {
        private LocalStatus()
        {
        }

        /// <summary>
        /// Gets a value indicating whether this PD is tamper.
        /// </summary>
        /// <value><c>true</c> if tamper; otherwise, <c>false</c>.</value>
        public bool Tamper { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this PD is experiencing a power failure.
        /// </summary>
        /// <value><c>true</c> if power failure; otherwise, <c>false</c>.</value>
        public bool PowerFailure { get; private set; }

        internal static LocalStatus ParseData(ReadOnlySpan<byte> data)
        {
            var dataArray = data.ToArray();
            if (dataArray.Length < 2)
            {
                throw new Exception("Invalid size for the data");
            }

            var localStatus = new LocalStatus
            {
                Tamper = Convert.ToBoolean(dataArray[0]),
                PowerFailure = Convert.ToBoolean(dataArray[1])
            };

            return localStatus;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"       Tamper: {Tamper}");
            build.AppendLine($"Power Failure: {PowerFailure}");

            return build.ToString();
        }
    }
}