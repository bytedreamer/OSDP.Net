using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace OSDP.Net
{
    internal class SecureChannel
    {
        private readonly byte[] _defaultSecureChannelKey =
            {0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F};

        private readonly byte[] _serverRandomNumber = new byte[8];
        private byte[] _cmac = new byte[16];
        private byte[] _enc = new byte[16];
        private byte[] _rmac = new byte[16];
        private byte[] _smac1 = new byte[16];
        private byte[] _smac2 = new byte[16];

        public SecureChannel()
        {
            Reset();
        }

        public bool IsInitialized { get; private set; }

        public bool IsEstablished { get; private set; }

        public IEnumerable<byte> ServerRandomNumber() => _serverRandomNumber;

        public byte[] Initialize(byte[] cUID, byte[] clientRandomNumber, byte[] clientCryptogram)
        {
            using (var keyAlgorithm = CreateKeyAlgorithm())
            {
                _enc = GenerateKey(keyAlgorithm,
                    new byte[]
                    {
                        0x01, 0x82, _serverRandomNumber[0], _serverRandomNumber[1], _serverRandomNumber[2],
                        _serverRandomNumber[3], _serverRandomNumber[4], _serverRandomNumber[5]
                    }, new byte[8], _defaultSecureChannelKey);

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
                    }, new byte[8], _defaultSecureChannelKey);
                _smac2 = GenerateKey(keyAlgorithm,
                    new byte[]
                    {
                        0x01, 0x02, _serverRandomNumber[0], _serverRandomNumber[1], _serverRandomNumber[2],
                        _serverRandomNumber[3], _serverRandomNumber[4], _serverRandomNumber[5]
                    }, new byte[8], _defaultSecureChannelKey);

                IsInitialized = true;
                return GenerateKey(keyAlgorithm, clientRandomNumber, _serverRandomNumber, _enc);
            }
        }

        public void Establish(byte[] rmac)
        {
            _rmac = rmac;
            IsEstablished = true;
        }

        public byte[] GenerateMac(byte[] buffer)
        {
            var encryptBuffer = new byte[16];
            buffer.CopyTo(encryptBuffer, 0);
            encryptBuffer[buffer.Length] = 0x80; 
            
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
                messageAuthenticationCodeAlgorithm.Key = _smac2;
                messageAuthenticationCodeAlgorithm.IV = _rmac;
            
                using (var encryptor = messageAuthenticationCodeAlgorithm.CreateEncryptor())
                {
                    _cmac = encryptor.TransformFinalBlock(encryptBuffer, 0, encryptBuffer.Length);
                    return _cmac;
                }
            }
        }

        public void Reset()
        {
            new Random().NextBytes(_serverRandomNumber);
            IsInitialized = false;
            IsEstablished = false;
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

        private byte[] GenerateKey(SymmetricAlgorithm algorithm, byte[] first, byte[] second, byte[] key)
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