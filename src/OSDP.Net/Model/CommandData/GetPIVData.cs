using System;
using System.Collections.Generic;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Command data to get the PIV data
    /// </summary>
    public class GetPIVData
    {
        private bool _useSingleByteOffset;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetPIVData"/> class.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="elementId">The element identifier.</param>
        /// <param name="dataOffset">The data offset.</param>
        [Obsolete("Single byte offset no longer supported with future versions of OSDP")]
        public GetPIVData(ObjectId objectId, byte elementId, byte dataOffset)
        {
            ObjectId = objectId switch
            {
                CommandData.ObjectId.CardholderUniqueIdentifier => new byte[] { 0x5F, 0xC1, 0x02 },
                CommandData.ObjectId.CertificateForPIVAuthentication => new byte[] { 0x5F, 0xC1, 0x05 },
                CommandData.ObjectId.CertificateForCardAuthentication => new byte[] { 0xDF, 0xC1, 0x01 },
                CommandData.ObjectId.CardholderFingerprintTemplate => new byte[] { 0xDF, 0xC1, 0x03 },
                _ => throw new ArgumentOutOfRangeException()
            };

            ElementId = elementId;
            DataOffset = dataOffset;

            _useSingleByteOffset = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetPIVData"/> class.
        /// </summary>
        /// <param name="objectId">The object identifier with a length of 3.</param>
        /// <param name="elementId">The element identifier.</param>
        /// <param name="dataOffset">The data offset.</param>
        public GetPIVData(byte[] objectId, byte elementId, ushort dataOffset)
        {
            if (objectId.Length != 3)
            {
                throw new ArgumentException("Object ID byte length must be 3", nameof(objectId));
            }
            
            ObjectId = objectId;
            ElementId = elementId;
            DataOffset = dataOffset;
        }

        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        public byte[] ObjectId { get; }

        /// <summary>
        /// Gets the element identifier.
        /// </summary>
        public byte ElementId { get; }

        /// <summary>
        /// Gets the data offset.
        /// </summary>
        public ushort DataOffset { get; }

        public static GetPIVData ParseData(ReadOnlySpan<byte> data)
        {
            if (data.Length < 5 || data.Length > 6)
            {
                throw new ArgumentException("Invalid data length, must either be 5 or 6 bytes", nameof(data));
            }

            var isSingleByteOffset = data.Length == 5;
            ushort offset = isSingleByteOffset ? data[4] :
                Message.ConvertBytesToUnsignedShort(data.Slice(4, 2));

            var res = new GetPIVData(data.Slice(0, 3).ToArray(), data[3], offset);
            res._useSingleByteOffset = isSingleByteOffset;
            return res;
        }

        /// <summary>
        /// Builds the data.
        /// </summary>
        /// <returns>The data.</returns>
        public ReadOnlySpan<byte> BuildData()
        {
            var data = new List<byte>();
            data.AddRange(ObjectId);
            data.Add(ElementId);
            if (_useSingleByteOffset)
                data.Add(Message.ConvertShortToBytes(DataOffset)[0]);
            else
                data.AddRange(Message.ConvertShortToBytes(DataOffset));
            return data.ToArray();
        }

        public override string ToString() => ToString(0);
        public string ToString(int indent = 0)
        {
            var padding = new string(' ', indent);
            var build = new StringBuilder();
            build.AppendLine($"{padding}  Object ID: {BitConverter.ToString(ObjectId)}");
            build.AppendLine($"{padding} Element ID: {ElementId}");
            build.AppendLine($"{padding}Data Offset: {DataOffset}");
            return build.ToString();
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