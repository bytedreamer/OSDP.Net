using System;
using System.Linq;
using System.Security.Cryptography;

namespace OSDP.Net.Messages.SecureChannel;

/// <summary>
/// Security context used within MessageSecureChannel
/// 
/// This state data is placed into its own class to facilitate use cases where multiple channels
/// (i.e. one for incoming packets; one for outgoing) have to share the same security state.
/// </summary>
public class SecurityContext
{
    private readonly byte[] _securityKey;

    // Todo - make private when conversion is done
    public static readonly byte[] DefaultKey = "0123456789:;<=>?"u8.ToArray();

    public SecurityContext(byte[] securityKey = null)
    {
        CreateNewRandomNumber();
        _securityKey = securityKey ?? DefaultKey;

        IsInitialized = false;
        IsSecurityEstablished = false;
    }

    /// <summary>
    /// A flag indicating whether or not channel security has been established
    /// </summary>
    public bool IsSecurityEstablished { get; set; }

    /// <summary>
    /// Symmertric message encryption key established by the secure channel handshake
    /// </summary>
    public byte[] Enc { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// S-MAC1 value
    /// </summary>
    public byte[] SMac1 { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// S-MAC2 value
    /// </summary>
    public byte[] SMac2 { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// R-MAC value
    /// </summary>
    public byte[] RMac { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// C-MAC value
    /// </summary>
    public byte[] CMac { get; set; } = Array.Empty<byte>();
    
    public byte[] ServerCryptogram { get; private set; }
    
    public byte[] ServerRandomNumber { get; } = new byte[8];

    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Creates a new instance of AES cypher
    /// </summary>
    /// <param name="isForSessionSetup">We use the cypher in two major use cases: 
    /// session setup and message data encryption. Depending on the case, it has 
    /// to be initialized slightly differently so this flag indicates which case 
    /// is currently needed.</param>
    /// <returns>Cypher instance</returns>
    public Aes CreateCypher(bool isForSessionSetup, byte[] key = null)
    {
        var crypto = Aes.Create();
        if (crypto == null)
        {
            throw new Exception("Unable to create key algorithm");
        }

        if (!isForSessionSetup)
        {
            crypto.Mode = CipherMode.CBC;
            crypto.Padding = PaddingMode.None;
        }
        else
        {
            crypto.Mode = CipherMode.ECB;
            crypto.Padding = PaddingMode.Zeros;
        }
        crypto.KeySize = 128;
        crypto.BlockSize = 128;
        crypto.Key = key ?? _securityKey;

        return crypto;
    }

    /// <summary>
    /// Slightly specialized version of simple AES encryption that is 
    /// intended specifically for generating keys used in OSDP secure channel
    /// comms. 
    /// </summary>
    /// <param name="aes">AES crypto instance</param>
    /// <param name="input">Set of bytes to be used as input to generate the 
    /// resulting key. For convenience the caller might pass in more than one
    /// byte array, but the total sum of all bytes MUST be less than or equal
    /// to 16</param>
    /// <returns></returns>
    public static byte[] GenerateKey(Aes aes, params byte[][] input)
    {
        var buffer = new byte[16];
        int currentSize = 0;
        foreach (byte[] x in input)
        {
            x.CopyTo(buffer, currentSize);
            currentSize += x.Length;
        }
        using var encryptor = aes.CreateEncryptor();

        return encryptor.TransformFinalBlock(buffer, 0, buffer.Length);
    }
    
    public void InitializeACU(byte[] clientRandomNumber, byte[] clientCryptogram)
    {
        using var keyAlgorithm = CreateCypher(true);
        Enc = GenerateKey(keyAlgorithm, new byte[]
        {
            0x01, 0x82, ServerRandomNumber[0], ServerRandomNumber[1], ServerRandomNumber[2],
            ServerRandomNumber[3], ServerRandomNumber[4], ServerRandomNumber[5]
        });

        using var serverCypher  = CreateCypher(true, Enc);
        if (!clientCryptogram.SequenceEqual(GenerateKey(serverCypher, 
                ServerRandomNumber, clientRandomNumber)))
        {
            throw new Exception("Invalid client cryptogram");
        }

        SMac1 = GenerateKey(keyAlgorithm,
            new byte[]
            {
                0x01, 0x01, ServerRandomNumber[0], ServerRandomNumber[1], ServerRandomNumber[2],
                ServerRandomNumber[3], ServerRandomNumber[4], ServerRandomNumber[5]
            });
        SMac2 = GenerateKey(keyAlgorithm,
            new byte[]
            {
                0x01, 0x02, ServerRandomNumber[0], ServerRandomNumber[1], ServerRandomNumber[2],
                ServerRandomNumber[3], ServerRandomNumber[4], ServerRandomNumber[5]
            });
                
        ServerCryptogram = GenerateKey(serverCypher, clientRandomNumber, ServerRandomNumber);
        IsInitialized = true;
    }
    
    public void CreateNewRandomNumber()
    {
        // Todo - this might be needed
        // IsInitialized = false;
        // IsEstablished = false;
        new Random().NextBytes(ServerRandomNumber);
    }
    
    public void Establish(byte[] rmac)
    {
        RMac = rmac;
        IsSecurityEstablished = true;
    }
}