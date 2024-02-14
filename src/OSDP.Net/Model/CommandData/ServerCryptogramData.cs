using System;
using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Model.CommandData;

public class ServerCryptogramData : CommandData
{
    public ServerCryptogramData(byte[] serverCryptogram, bool isDefaultKey)
    {
        ServerCryptogram = serverCryptogram ?? throw new ArgumentNullException(nameof(serverCryptogram));;
        IsDefaultKey = isDefaultKey;
    }
    
    public byte[] ServerCryptogram { get; }
    
    public bool IsDefaultKey { get; }
    
    /// <inheritdoc />
    public override CommandType CommandType => CommandType.ServerCryptogram;

    /// <inheritdoc />
    public override byte Code => (byte)CommandType;
        
    /// <inheritdoc />
    internal override ReadOnlySpan<byte> SecurityControlBlock()
    {
        return new byte[]
        {
            0x03,
            (byte)SecurityBlockType.SecureConnectionSequenceStep3,
            (byte)(IsDefaultKey ? 0x00 : 0x01)
        };
    }

    /// <inheritdoc />
    public override byte[] BuildData()
    {
        return ServerCryptogram;
    }

    /// <summary>
    /// Parses the message payload bytes
    /// </summary>
    /// <param name="data">Message payload as bytes</param>
    /// <param name="securityControlBlock">Security control block as bytes</param>
    /// <returns>An instance of ServerCryptogram representing the message payload</returns>
    public static ServerCryptogramData ParseData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> securityControlBlock)
    {
        return new ServerCryptogramData(data.ToArray(), securityControlBlock[2] == 0x01);
    }
}