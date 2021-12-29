using System;
using System.Text;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// A local status report reply.
    /// </summary>
    public class BiometricMatchResult
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="BiometricMatchResult"/> class from being created.
        /// </summary>
        private BiometricMatchResult()
        {
        }

        /// <summary>
        /// Gets the reader number 0=First Reader 1=Second Reader
        /// </summary>
        public byte ReaderNumber { get; private set; }

        /// <summary>
        /// Gets the results of the PD performing the biometric scan.
        /// </summary>
        public BiometricStatus Status { get; private set; }

        /// <summary>
        /// Gets biometric score with no match at 0x00 and best match at 0xFF.
        /// </summary>
        public byte Score { get; private set; }

        /// <summary>Parses the data.</summary>
        /// <param name="data">The data.</param>
        /// <returns>A biometric result reply.</returns>
        /// <exception cref="System.Exception">Invalid size for the data</exception>
        internal static BiometricMatchResult ParseData(ReadOnlySpan<byte> data)
        {
            var dataArray = data.ToArray();
            if (dataArray.Length != 3)
            {
                throw new Exception("Invalid size for the data");
            }

            var biometricResult = new BiometricMatchResult
            {
                ReaderNumber = dataArray[0],
                Status = typeof(BiometricStatus).IsEnumDefined((int)data[1])
                    ? (BiometricStatus)data[1]
                    : BiometricStatus.UnknownError,
                Score = dataArray[2]
            };

            return biometricResult;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"Reader Number: {ReaderNumber}");
            build.AppendLine($"       Status: {Status}");
            build.AppendLine($"        Score: {Score}");

            return build.ToString();
        }
    }

    /// <summary>
    /// Results of the biometric scanning.
    /// </summary>
    public enum BiometricStatus
    {
#pragma warning disable CS1591
        Success = 0x00,
        Timeout = 0x01,
        UnknownError = 0xFF
#pragma warning restore CS1591
    }
}