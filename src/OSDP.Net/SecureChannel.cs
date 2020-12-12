using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace OSDP.Net
{
    internal class SecureChannel
    {
        private readonly byte[] _serverRandomNumber = new byte[8];
        private byte[] _cmac = new byte[16];
        private byte[] _enc = new byte[16];
        private byte[] _rmac = new byte[16];
        private byte[] _smac1 = new byte[16];
        private byte[] _smac2 = new byte[16];

        public SecureChannel()
        {
            new Random().NextBytes(_serverRandomNumber);
            IsInitialized = false;
            IsEstablished = false;
        }

        public byte[] ServerCryptogram { get; private set; }

        public bool IsInitialized { get; private set; }

        public bool IsEstablished { get; private set; }

        public IEnumerable<byte> ServerRandomNumber() => _serverRandomNumber;

        public void Initialize(byte[] clientRandomNumber, byte[] clientCryptogram, byte[] secureChannelKey)
        {
            using (var keyAlgorithm = CreateKeyAlgorithm())
            {
                _enc = GenerateKey(keyAlgorithm,
                    new byte[]
                    {
                        0x01, 0x82, _serverRandomNumber[0], _serverRandomNumber[1], _serverRandomNumber[2],
                        _serverRandomNumber[3], _serverRandomNumber[4], _serverRandomNumber[5]
                    }, new byte[8], secureChannelKey);

                if (!clientCryptogram.SequenceEqual(GenerateKey(keyAlgorithm, 
                    _serverRandomNumber, clientRandomNumber, _enc)))
                {
                    throw new Exception("Invalid client cryptogram");
                }

                _smac1 = GenerateKey(keyAlgorithm,
                    new byte[]
                    {
                        0x01, 0x01, _serverRandomNumber[0], _serverRandomNumber[1], _serverRandomNumber[2],
                        _serverRandomNumber[3], _serverRandomNumber[4], _serverRandomNumber[5]
                    }, new byte[8], secureChannelKey);
                _smac2 = GenerateKey(keyAlgorithm,
                    new byte[]
                    {
                        0x01, 0x02, _serverRandomNumber[0], _serverRandomNumber[1], _serverRandomNumber[2],
                        _serverRandomNumber[3], _serverRandomNumber[4], _serverRandomNumber[5]
                    }, new byte[8], secureChannelKey);
                
                ServerCryptogram = GenerateKey(keyAlgorithm, clientRandomNumber, _serverRandomNumber, _enc);
                IsInitialized = true;
            }
        }

        public void Establish(byte[] rmac)
        {
            _rmac = rmac;
            IsEstablished = true;
        }

        public byte[] GenerateMac(ReadOnlySpan<byte> message, bool isCommand)
        {
            const byte cryptoLength = 16;
            const byte paddingStart = 0x80;

            var mac = new byte[cryptoLength];
            int currentLocation = 0;

            using (var messageAuthenticationCodeAlgorithm = Aes.Create())
            {
                if (messageAuthenticationCodeAlgorithm == null)
                {
                    throw new Exception("Unable to create key algorithm");
                }

                messageAuthenticationCodeAlgorithm.Mode = CipherMode.CBC;
                messageAuthenticationCodeAlgorithm.KeySize = 128;
                messageAuthenticationCodeAlgorithm.BlockSize = 128;
                messageAuthenticationCodeAlgorithm.Padding = PaddingMode.None;
                messageAuthenticationCodeAlgorithm.IV = isCommand ? _rmac : _cmac;
                messageAuthenticationCodeAlgorithm.Key = _smac1;

                int messageLength = message.Length;
                while (currentLocation < messageLength)
                {
                    // Get first 16
                    var inputBuffer = new byte[cryptoLength];
                    message.Slice(currentLocation,
                            currentLocation + cryptoLength < messageLength
                                ? cryptoLength
                                : messageLength - currentLocation)
                        .CopyTo(inputBuffer);

                    currentLocation += cryptoLength;
                    if (currentLocation > messageLength)
                    {
                        messageAuthenticationCodeAlgorithm.Key = _smac2;
                        if (messageLength % cryptoLength != 0)
                        {
                            inputBuffer[messageLength % cryptoLength] = paddingStart;
                        }
                    }

                    using (var encryptor = messageAuthenticationCodeAlgorithm.CreateEncryptor())
                    {
                        mac = encryptor.TransformFinalBlock(inputBuffer.ToArray(), 0, inputBuffer.Length);
                    }

                    messageAuthenticationCodeAlgorithm.IV = mac;
                }

                if (isCommand)
                {
                    _cmac = mac;
                }
                else
                {
                    _rmac = mac;
                }

                return mac;
            }
        }

        public IEnumerable<byte> DecryptData(ReadOnlySpan<byte> data)
        {
            const byte paddingStart = 0x80;
            
            using (var messageAuthenticationCodeAlgorithm = Aes.Create())
            {
                if (messageAuthenticationCodeAlgorithm == null)
                {
                    throw new Exception("Unable to create key algorithm");
                }

                messageAuthenticationCodeAlgorithm.Mode = CipherMode.CBC;
                messageAuthenticationCodeAlgorithm.KeySize = 128;
                messageAuthenticationCodeAlgorithm.BlockSize = 128;
                messageAuthenticationCodeAlgorithm.Padding = PaddingMode.None;
                messageAuthenticationCodeAlgorithm.IV = _cmac.Select(b => (byte) ~b).ToArray();
                messageAuthenticationCodeAlgorithm.Key = _enc;

                List<byte> decryptedData = new List<byte>();
                
                using (var encryptor = messageAuthenticationCodeAlgorithm.CreateDecryptor())
                {
                    decryptedData.AddRange(encryptor.TransformFinalBlock(data.ToArray(), 0, data.Length));
                }
                
                while (decryptedData.Any() && decryptedData.Last() != paddingStart)
                {
                    decryptedData.RemoveAt(decryptedData.Count - 1);
                }
                
                if (decryptedData.Any() && decryptedData.Last() == paddingStart)
                {
                    decryptedData.RemoveAt(decryptedData.Count - 1);
                }

                return decryptedData;
            } 
        }

        public ReadOnlySpan<byte> EncryptData(ReadOnlySpan<byte> data)
        {
            const byte cryptoLength = 16;
            const byte paddingStart = 0x80;
            
            using (var messageAuthenticationCodeAlgorithm = Aes.Create())
            {
                if (messageAuthenticationCodeAlgorithm == null)
                {
                    throw new Exception("Unable to create key algorithm");
                }

                messageAuthenticationCodeAlgorithm.Mode = CipherMode.CBC;
                messageAuthenticationCodeAlgorithm.KeySize = 128;
                messageAuthenticationCodeAlgorithm.BlockSize = 128;
                messageAuthenticationCodeAlgorithm.Padding = PaddingMode.None;
                messageAuthenticationCodeAlgorithm.IV = _rmac.Select(b => (byte) ~b).ToArray();
                messageAuthenticationCodeAlgorithm.Key = _enc;

                var paddedData = PadTheData(data, cryptoLength, paddingStart);

                using (var encryptor = messageAuthenticationCodeAlgorithm.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(paddedData, 0, paddedData.Length);
                }
            }
        }

        private static byte[] PadTheData(ReadOnlySpan<byte> data, byte cryptoLength, byte paddingStart)
        {
            int dataLength = data.Length + 1;
            int paddingLength = dataLength + (cryptoLength - (dataLength % cryptoLength)) % cryptoLength;
            
            Span<byte> buffer = stackalloc byte[paddingLength];
            var cursor = buffer.Slice(0);

            data.CopyTo(cursor);
            cursor = cursor.Slice(data.Length);
            
            cursor[data.Length] = paddingStart;
            
            return buffer.ToArray();
        }

        private static Aes CreateKeyAlgorithm()
        {
            var keyAlgorithm = Aes.Create();
            if (keyAlgorithm == null)
            {
                throw new Exception("Unable to create key algorithm");
            }

            keyAlgorithm.Mode = CipherMode.ECB;
            keyAlgorithm.KeySize = 128;
            keyAlgorithm.BlockSize = 128;
            keyAlgorithm.Padding = PaddingMode.Zeros;
            return keyAlgorithm;
        }

        private static byte[] GenerateKey(SymmetricAlgorithm algorithm, byte[] first, byte[] second, byte[] key)
        {
            var buffer = new byte[16];
            first.CopyTo(buffer, 0);
            second.CopyTo(buffer, 8);
            
            algorithm.Key = key;
            using (var encryptor = algorithm.CreateEncryptor())
            {
                return encryptor.TransformFinalBlock(buffer, 0, buffer.Length);
            }
        }
    }
}