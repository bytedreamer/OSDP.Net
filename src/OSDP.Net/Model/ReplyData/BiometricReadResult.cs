using System;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// A biometric read reply.
    /// </summary>
    public class BiometricReadResult
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="BiometricMatchResult"/> class from being created.
        /// </summary>
        private BiometricReadResult()
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
        /// Gets the type/body part that was scanned.
        /// </summary>
        public BiometricType Type { get; private set; }

        /// <summary>
        /// The scan quality with 0x00 as worse and 0x99 as best.
        /// </summary>
        public byte Quality { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public short Length { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public byte[] TemplateData { get; private set; }

        /// <summary>Parses the data.</summary>
        /// <param name="data">The data.</param>
        /// <returns>A biometric result reply.</returns>
        /// <exception cref="System.Exception">Invalid size for the data</exception>
        internal static BiometricReadResult ParseData(ReadOnlySpan<byte> data)
        {
            var dataArray = data.ToArray();

            var biometricResult = new BiometricReadResult
            {
                ReaderNumber = dataArray[0],
                Status = typeof(BiometricStatus).IsEnumDefined((int)data[1])
                    ? (BiometricStatus)data[1]
                    : BiometricStatus.UnknownError,
                Type = (BiometricType)dataArray[2],
                Quality = dataArray[3],
                Length = Message.ConvertBytesToShort(dataArray.Skip(4).Take(2).ToArray(), true),
                TemplateData = dataArray.Skip(6).ToArray()
            };

            return biometricResult;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"Reader Number: {ReaderNumber}");
            build.AppendLine($"       Status: {Status}");
            build.AppendLine($"         Type: {Type}");
            build.AppendLine($"      Quality: {Quality}");
            build.AppendLine($"       Length: {Length}");

            return build.ToString();
        }
    }
}