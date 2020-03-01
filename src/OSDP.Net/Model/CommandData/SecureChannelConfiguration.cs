using System.Collections.Generic;
using System.Linq;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    public class EncryptionKeyConfiguration
    {
        public EncryptionKeyConfiguration(KeyType keyType, byte[] keyData)
        {
            KeyType = keyType;
            KeyData = keyData;
        }

        public KeyType KeyType { get; }

        public byte[] KeyData { get; }

        public IEnumerable<byte> BuildData()
        {
            var data = new List<byte>
            {
                (byte)KeyType,
                (byte)KeyData.Length
            };
            data.AddRange(KeyData);

            return data;
        }
    }

    public enum KeyType
    {
        SecureChannelBaseKey = 0x01
    }
}