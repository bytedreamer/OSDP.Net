using System;
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
    public static readonly byte[] DefaultKey = "0123456789:;<=>?"u8.ToArray();
    
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

    /// <summary>
    /// Creates a new instance of AES cypher
    /// </summary>
    /// <param name="key">Encryption key to be used</param>
    /// <param name="isForSessionSetup">We use the cypher in two major use cases: 
    /// session setup and message data encryption. Depending on the case, it has 
    /// to be initialized slightly differently so this flag indicates which case 
    /// is currently needed.</param>
    /// <returns>Cypher instance</returns>
    public static Aes CreateCypher(byte[] key, bool isForSessionSetup)
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
        crypto.Key = key;

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
}