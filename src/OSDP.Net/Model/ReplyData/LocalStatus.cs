using System;
using System.Text;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// A local status report reply.
    /// </summary>
    public class LocalStatus
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="LocalStatus"/> class from being created.
        /// </summary>
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

        /// <summary>Parses the data.</summary>
        /// <param name="data">The data.</param>
        /// <returns>A local status report reply.</returns>
        /// <exception cref="System.Exception">Invalid size for the data</exception>
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