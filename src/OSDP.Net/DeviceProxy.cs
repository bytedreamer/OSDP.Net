using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OSDP.Net.Messages.ACU;
using OSDP.Net.Messages.SecureChannel;
using Reply = OSDP.Net.Messages.ACU.Reply;

namespace OSDP.Net;

/// <summary>
/// 
/// </summary>
public class DeviceProxy : IComparable<DeviceProxy>,IDisposable
{
    private const int RetryAmount = 2;

    private readonly ConcurrentQueue<Command> _commands = new();

    private readonly SecureChannel _secureChannel = new();
    private readonly bool _useSecureChannel;
    private readonly ILogger<DeviceProxy> _logger;

    private int _counter = RetryAmount;
    private DateTime _lastValidReply = DateTime.MinValue;

    private Command _retryCommand;

    /// <summary>
    /// Represents a device with a specific address.
    /// </summary>
    /// <param name="address">The address of the device.</param>
    /// <param name="useCrc">Specifies whether to use CRC (Cyclic Redundancy Check) for message validation.</param>
    /// <param name="useSecureChannel">Specifies whether to use a secure channel for communication.</param>
    /// <param name="secureChannelKey">The key used for securing the communication channel.</param>
    /// <param name="logger">The logger used for logging purposes.</param>
    public DeviceProxy(byte address, bool useCrc, bool useSecureChannel, byte[] secureChannelKey = null,
        ILogger<DeviceProxy> logger = null)
    {
        _useSecureChannel = useSecureChannel;
        _logger = logger;

        Address = address;
        MessageControl = new Control(0, useCrc, useSecureChannel);

        if (UseSecureChannel)
        {
            SecureChannelKey = secureChannelKey ?? SecurityContext.DefaultKey;
            
            IsDefaultKey = SecurityContext.DefaultKey.SequenceEqual(SecureChannelKey);
        }
    }

    internal byte[] SecureChannelKey { get; }

    private bool IsDefaultKey { get; }

    public byte Address { get; }

    internal Control MessageControl { get; }

    public bool UseSecureChannel => !IsSendingMultiMessageNoSecureChannel && _useSecureChannel;

    public bool IsSecurityEstablished => !IsSendingMultiMessageNoSecureChannel && MessageControl.HasSecurityControlBlock && _secureChannel.IsEstablished;

    public bool IsConnected => _lastValidReply + TimeSpan.FromSeconds(8) >= DateTime.UtcNow &&
                               (IsSendingMultiMessageNoSecureChannel || !MessageControl.HasSecurityControlBlock || IsSecurityEstablished);

    internal bool IsSendingMultipartMessage { get; set; }

    internal DateTime RequestDelay { get; set; }

    internal bool IsSendingMultiMessageNoSecureChannel { get; set; }
        
    internal bool IsReceivingMultipartMessage { get; set; }
        
    /// <summary>
    /// Has one or more commands waiting in the queue
    /// </summary>
    internal bool HasQueuedCommand => _commands.Any();

    public void Dispose() {}

    /// <inheritdoc />
    public int CompareTo(DeviceProxy other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return Address.CompareTo(other.Address);
    }

    /// <summary>
    /// Get the next command in the queue, setup security, or send a poll command
    /// </summary>
    /// <param name="isPolling">If false, only send commands in the queue</param>
    /// <returns>The next command always if polling, could be null if not polling</returns>
    internal Command GetNextCommandData(bool isPolling)
    {
        if (_retryCommand != null)
        {
            var saveCommand = _retryCommand;
            _retryCommand = null;
            return saveCommand;
        }
            
        if (isPolling)
        {
            // Don't send clear text polling if using secure channel
            if (MessageControl.Sequence == 0 && !UseSecureChannel)
            {
                return new PollCommand(Address);
            }
                
            if (UseSecureChannel && !_secureChannel.IsInitialized)
            {
                return new SecurityInitializationRequestCommand(Address,
                    _secureChannel.ServerRandomNumber().ToArray(), IsDefaultKey);
            }

            if (UseSecureChannel && !_secureChannel.IsEstablished)
            {
                return new ServerCryptogramCommand(Address, _secureChannel.ServerCryptogram, IsDefaultKey);
            }
        }

        if (!_commands.TryDequeue(out var command) && isPolling)
        {
            return new PollCommand(Address);
        }

        return command;
    }

    internal void SendCommand(Command command)
    {
        _commands.Enqueue(command);
    }

    /// <summary>
    /// Store command for retry
    /// </summary>
    /// <param name="command"></param>
    internal void RetryCommand(Command command)
    {
        if (_counter-- > 0)
        {
            _retryCommand = command;
        }
        else
        {
            _retryCommand = null;
            _counter = RetryAmount;
        }
    }

    internal void ValidReplyHasBeenReceived(byte sequence)
    {
        MessageControl.IncrementSequence(sequence);
            
        // It's valid once sequences are above zero
        if (sequence > 0) _lastValidReply = DateTime.UtcNow;
            
        // Reset retry counter
        _counter = RetryAmount;
    }

    internal void InitializeSecureChannel(Reply reply)
    {
        var replyData = reply.ExtractReplyData.ToArray();

        _secureChannel.Initialize(replyData.Skip(8).Take(8).ToArray(),
            replyData.Skip(16).Take(16).ToArray(), SecureChannelKey);
    }

    internal bool ValidateSecureChannelEstablishment(Reply reply)
    {
        if (!reply.SecureCryptogramHasBeenAccepted())
        {
            return false;
        }

        _secureChannel.Establish(reply.ExtractReplyData.ToArray());

        return true;
    }

    internal ReadOnlySpan<byte> GenerateMac(ReadOnlySpan<byte> message, bool isCommand)
    {
        return _secureChannel.GenerateMac(message, isCommand);
    }

    internal ReadOnlySpan<byte> EncryptData(ReadOnlySpan<byte> data)
    {
        return _secureChannel.EncryptData(data);
    }

    internal IEnumerable<byte> DecryptData(ReadOnlySpan<byte> data)
    {
        return _secureChannel.DecryptData(data);
    }

    internal void CreateNewRandomNumber()
    {
        _secureChannel.CreateNewRandomNumber();
    }
}
