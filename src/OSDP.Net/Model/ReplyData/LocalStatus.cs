using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;
using System;
using System.Text;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// A local status report reply.
    /// </summary>
    public class LocalStatus : PayloadData
    {
        /// <summary>
        /// Initializes a new instance of LocalStatus class
        /// </summary>
        /// <param name="tamper"></param>
        /// <param name="powerFailure"></param>
        public LocalStatus(bool tamper, bool powerFailure)
        {
            Tamper = tamper;
            PowerFailure = powerFailure;
        }

        /// <inheritdoc />
        public override byte Code => (byte)ReplyType.LocalStatusReport;

        /// <summary>
        /// Gets a value indicating whether this PD is tamper.
        /// </summary>
        /// <value><c>true</c> if tamper; otherwise, <c>false</c>.</value>
        public bool Tamper { get; }

        /// <summary>
        /// Gets a value indicating whether this PD is experiencing a power failure.
        /// </summary>
        /// <value><c>true</c> if power failure; otherwise, <c>false</c>.</value>
        public bool PowerFailure { get; }

        /// <inheritdoc />
        public override ReadOnlySpan<byte> SecurityControlBlock() => SecurityBlock.ReplyMessageWithDataSecurity;

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

            return new LocalStatus(
                Convert.ToBoolean(dataArray[0]), Convert.ToBoolean(dataArray[1]));
        }

        /// <inheritdoc />
        public override byte[] BuildData() => [
            Tamper ? (byte)0x01 : (byte)0x00, 
            PowerFailure ? (byte)0x01 : (byte)0x00];

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