using System.Collections.Generic;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Command data to send a request to the PD to perform a biometric scan and match.
    /// </summary>
    public class BiometricReadData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BiometricReadData"/> class.
        /// </summary>
        /// <param name="readerNumber">The reader number starting at 0.</param>
        /// <param name="type">The type/body part to scan.</param>
        /// <param name="format">The format of the attached template.</param>
        /// <param name="quality">.</param>
        public BiometricReadData(byte readerNumber, BiometricType type, BiometricFormat format,
            byte quality)
        {
            ReaderNumber = readerNumber;
            Type = type;
            Format = format;
            Quality = quality;
        }

        /// <summary>
        /// Gets the reader number starting at 0.
        /// </summary>
        public byte ReaderNumber { get; }

        /// <summary>
        /// Gets the type/body part to scan.
        /// </summary>
        public BiometricType Type { get; }

        /// <summary>
        /// Gets the format of the attached template.
        /// </summary>
        public BiometricFormat Format { get; }

        /// <summary>
        /// Gets the ?.
        /// </summary>
        public byte Quality { get; }

        internal IEnumerable<byte> BuildData()
        {
            return new []
            {
                ReaderNumber,
                (byte)Type,
                (byte)Format,
                Quality
            };
        }
    }
}