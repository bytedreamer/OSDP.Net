using System;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Command data to get the PIV data
    /// </summary>
    public class GetPIVData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetPIVData"/> class.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="elementId">The element identifier.</param>
        /// <param name="dataOffset">The data offset.</param>
        public GetPIVData(ObjectId objectId, byte elementId, byte dataOffset)
        {
            ObjectId = objectId;
            ElementId = elementId;
            DataOffset = dataOffset;
        }

        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        /// <value>The object identifier.</value>
        public ObjectId ObjectId { get; }

        /// <summary>
        /// Gets the element identifier.
        /// </summary>
        /// <value>The element identifier.</value>
        public byte ElementId { get; }

        /// <summary>
        /// Gets the data offset.
        /// </summary>
        /// <value>The data offset.</value>
        public byte DataOffset { get; }

        /// <summary>
        /// Builds the data.
        /// </summary>
        /// <returns>The data.</returns>
        public ReadOnlySpan<byte> BuildData()
        {
            return ObjectId switch
            {
                ObjectId.CardholderUniqueIdentifier => new byte[] {0x5F, 0xC1, 0x02, ElementId, DataOffset},
                ObjectId.CertificateForPIVAuthentication => new byte[] {0x5F, 0xC1, 0x05, ElementId, DataOffset},
                ObjectId.CertificateForCardAuthentication => new byte[] {0xDF, 0xC1, 0x01, ElementId, DataOffset},
                ObjectId.CardholderFingerprintTemplate => new byte[] { 0xDF, 0xC1, 0x03, ElementId, DataOffset },
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    /// <summary>
    /// Enum ObjectId
    /// </summary>
    public enum ObjectId
    {
        /// <summary>
        /// The cardholder unique identifier
        /// </summary>
        CardholderUniqueIdentifier,
        /// <summary>
        /// The certificate for piv authentication
        /// </summary>
        CertificateForPIVAuthentication,
        /// <summary>
        /// The certificate for card authentication
        /// </summary>
        CertificateForCardAuthentication,
        /// <summary>
        /// The cardholder fingerprint template
        /// </summary>
        CardholderFingerprintTemplate
    }
}