using System;
using System.Collections.Generic;

namespace OSDP.Net.Model.CommandData
{
    public class GetPIVData
    {
        public GetPIVData(ObjectId objectId, byte elementId, byte dataOffset)
        {
            ObjectId = objectId;
            ElementId = elementId;
            DataOffset = dataOffset;
        }

        public ObjectId ObjectId { get; }

        public byte ElementId { get; }

        public byte DataOffset { get; }

        public IEnumerable<byte> BuildData()
        {
            return ObjectId switch
            {
                ObjectId.CardholderUniqueIdentifier => new byte[] {0x5F, 0xC1, 0x02, ElementId, DataOffset},
                ObjectId.CertificateForPIVAuthentication => new byte[] {0x5F, 0xC1, 0x05, ElementId, DataOffset},
                ObjectId.CertificateForCardAuthentication => new byte[] {0xDF, 0xC1, 0x01, ElementId, DataOffset},
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public enum ObjectId
    {
        CardholderUniqueIdentifier,
        CertificateForPIVAuthentication,
        CertificateForCardAuthentication
    }
}