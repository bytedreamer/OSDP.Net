using System;
using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Model.CommandData;

internal class SecurityInitialization : CommandData
{
    /// <inheritdoc />
    public SecurityInitialization(byte[] serverRandomNumber, bool isDefaultKey)
    {
        ServerRandomNumber = serverRandomNumber ?? throw new ArgumentNullException(nameof(serverRandomNumber));;
        IsDefaultKey = isDefaultKey;
    }
    
    public byte[] ServerRandomNumber { get; }
    
    public bool IsDefaultKey { get; }
    
    /// <inheritdoc />
    public override CommandType CommandType => CommandType.SessionChallenge;

    /// <inheritdoc />
    public override byte Code => (byte)CommandType;
    
    /// <inheritdoc />
    public override bool IsSecurityInitialization => true;
        
    /// <inheritdoc />
    public override ReadOnlySpan<byte> SecurityControlBlock()
    {
        return new byte[]
        {
            0x03,
            (byte)SecurityBlockType.BeginNewSecureConnectionSequence,
            (byte)(IsDefaultKey ? 0x00 : 0x01)
        };
    }

    /// <inheritdoc />
    public override byte[] BuildData()
    {
        return ServerRandomNumber;
    }

    /// <summary>
    /// Parses the message payload bytes
    /// </summary>
    /// <param name="data">Message payload as bytes</param>
    /// <param name="securityControlBlock">Security control block as bytes</param>
    /// <returns>An instance of SecurityInitialization representing the message payload</returns>
    public static SecurityInitialization ParseData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> securityControlBlock)
    {
        return new SecurityInitialization(data.ToArray(), securityControlBlock[2] == 0x01);
    }
}