using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OSDP.Net.Connections;
using OSDP.Net.Messages;
using OSDP.Net.Messages.ACU;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model;
using OSDP.Net.Model.ReplyData;

namespace OSDP.Net;

/// <summary>
/// 
/// </summary>
public class Device : IComparable<Device>, IDisposable
{
    private const int RetryAmount = 2;

    private readonly ConcurrentQueue<Command> _commands = new();

    public IMessageSecureChannel MessageSecureChannel { get; }
    
    private readonly bool _useSecureChannel;
    private readonly ILogger<Device> _logger;
    private readonly AutoResetEvent _shutdownComplete = new (false);
    
    private int _counter = RetryAmount;
    private DateTime _lastValidReply = DateTime.MinValue;
    private CancellationTokenSource _cancellationTokenSource;

    private Command _retryCommand;

    /// <summary>
    /// Represents a device with a specific address.
    /// </summary>
    /// <param name="address">The address of the device.</param>
    /// <param name="useCrc">Specifies whether to use CRC (Cyclic Redundancy Check) for message validation.</param>
    /// <param name="useSecureChannel">Specifies whether to use a secure channel for communication.</param>
    /// <param name="secureChannelKey">The key used for securing the communication channel.</param>
    /// <param name="logger">The logger used for logging purposes.</param>
    public Device(byte address, bool useCrc, bool useSecureChannel, byte[] secureChannelKey = null,
        ILogger<Device> logger = null)
    {
        _useSecureChannel = useSecureChannel;
        _logger = logger;

        Address = address;
        MessageControl = new Control(0, useCrc, useSecureChannel);

        if (UseSecureChannel)
        {
            SecureChannelKey = secureChannelKey ?? SecurityContext.DefaultKey;
            
            IsDefaultKey = SecurityContext.DefaultKey.SequenceEqual(SecureChannelKey);

            MessageSecureChannel = new ACUMessageSecureChannel(new SecurityContext(secureChannelKey));
        }
    }

    internal byte[] SecureChannelKey { get; }

    private bool IsDefaultKey { get; }

    public byte Address { get; }

    internal Control MessageControl { get; }

    public bool UseSecureChannel => !IsSendingMultiMessageNoSecureChannel && _useSecureChannel;

    public bool IsSecurityEstablished => !IsSendingMultiMessageNoSecureChannel && MessageControl.HasSecurityControlBlock && MessageSecureChannel.IsSecurityEstablished;

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

    /// <inheritdoc />
    public int CompareTo(Device other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return Address.CompareTo(other.Address);
    }

    /// <summary>
    /// Starts listening for incoming commands from the specified IOsdpConnection.
    /// </summary>
    /// <param name="connection">The IOsdpConnection to listen for commands.</param>
    /// <param name="commandProcessing">The ICommandProcessing instance to handle the incoming commands.</param>
    public void StartListening(IOsdpConnection connection, ICommandProcessing commandProcessing)
    {
        var cancellationTokenSource = _cancellationTokenSource;
        if (cancellationTokenSource != null) return;
        _cancellationTokenSource = cancellationTokenSource = new CancellationTokenSource();
        
        Task.Factory.StartNew(async () =>
        {
            var secureChannel = new PdMessageSecureChannel();

            try
            {
                connection.Open();
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var commandBuffer = await GetCommand(connection);
                    
                    if (commandBuffer.Length == 0) continue;

                    var incomingMessage = new IncomingMessage(commandBuffer, secureChannel);

                    HandleResponse(connection, secureChannel, incomingMessage, commandProcessing);

                    if (incomingMessage.Sequence > 0) _lastValidReply = DateTime.UtcNow;
                }
            }
            catch(Exception exception)
            {
                _logger?.LogError(exception, $"Unexpected exception in polling loop");
            }
        }, TaskCreationOptions.LongRunning);

        _shutdownComplete.Set();
    }

    private void HandleResponse(IOsdpConnection connection, PdMessageSecureChannel secureChannel, IncomingMessage incomingMessage, ICommandProcessing commandProcessing)
    {
        PayloadData replyData;

        switch ((CommandType)incomingMessage.Type)
        {
            case CommandType.Poll:
                replyData = commandProcessing.Poll();
                break;
            case CommandType.IdReport:
                replyData = commandProcessing.IdReport();
                break;
            default:
                replyData = new Nak(ErrorCode.UnknownCommandCode);
                break;
        }

        var reply = new OutgoingMessage(replyData);
        var data = reply.BuildMessage(incomingMessage.ControlBlock, secureChannel);
        var buffer = new byte[data.Length + 1];

        // Section 5.7 states that transmitting device shall guarantee an idle time between packets. This is
        // accomplished by sending a character with all bits set to 1. The driver byte is required by
        // converters and multiplexers to sense when line is idle.
        buffer[0] = Bus.DriverByte;
        Buffer.BlockCopy(data, 0, buffer, 1, data.Length);
            
        Debug.WriteLine("Outgoing: " + BitConverter.ToString(data));
        
        connection.WriteAsync(buffer);
    }

    private async Task<byte[]> GetCommand(IOsdpConnection connection)
    {
        var commandBuffer = new Collection<byte>();

        if (!await Bus.WaitForStartOfMessage(connection, commandBuffer, true, _cancellationTokenSource.Token)
                .ConfigureAwait(false))
        {
            return Array.Empty<byte>();
        }

        if (!await Bus.WaitForMessageLength(connection, commandBuffer, _cancellationTokenSource.Token).ConfigureAwait(false))
        {
            throw new TimeoutException("Timeout waiting for command message length");
        }

        if (!await Bus.WaitForRestOfMessage(connection, commandBuffer, Bus.ExtractMessageLength(commandBuffer),
                _cancellationTokenSource.Token).ConfigureAwait(false))
        {
            throw new TimeoutException("Timeout waiting for command of reply message");
        }
        
        Debug.WriteLine("Incoming: " + BitConverter.ToString(commandBuffer.ToArray()));
        
        return commandBuffer.ToArray();
    }

    public void StopListening()
    {
        var cancellationTokenSource = _cancellationTokenSource;
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            _shutdownComplete.WaitOne(TimeSpan.FromSeconds(1));
            _cancellationTokenSource = null;
        }
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
                
            if (UseSecureChannel && !MessageSecureChannel.IsInitialized)
            {
                return new SecurityInitializationRequestCommand(Address,
                    MessageSecureChannel.ServerRandomNumber, IsDefaultKey);
            }

            if (UseSecureChannel && !MessageSecureChannel.IsSecurityEstablished)
            {
                return new ServerCryptogramCommand(Address, MessageSecureChannel.ServerCryptogram, IsDefaultKey);
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

    internal void InitializeSecureChannel(byte[] payload)
    {
        MessageSecureChannel.InitializeACU(payload.Skip(8).Take(8).ToArray(),
            payload.Skip(16).Take(16).ToArray());
    }
    
    internal void ResetSecureChannelSession()
    {
        MessageSecureChannel.ResetSecureChannelSession();
    }

    internal ReadOnlySpan<byte> GenerateMac(ReadOnlySpan<byte> message, bool isIncoming)
    {
        return MessageSecureChannel.GenerateMac(message, isIncoming);
    }

    internal ReadOnlySpan<byte> EncryptData(ReadOnlySpan<byte> payload)
    {
        var paddedData = MessageSecureChannel.PadTheData(payload);
        
        var encryptedData = new Span<byte>(new byte[paddedData.Length]);
        MessageSecureChannel.EncodePayload(paddedData.ToArray(), encryptedData);
        return encryptedData;
    }

    internal IEnumerable<byte> DecryptData(ReadOnlySpan<byte> payload)
    {
        return MessageSecureChannel.DecodePayload(payload.ToArray());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _shutdownComplete?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}

public interface ICommandProcessing
{
    PayloadData Poll();

    PayloadData IdReport();
}