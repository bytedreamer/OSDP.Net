using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OSDP.Net.Connections;
using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Model.ReplyData;

namespace OSDP.Net;

public class Device : IDisposable
{
    private readonly ILogger _logger;
    private CancellationTokenSource _cancellationTokenSource;
    private Task _listenerTask = Task.CompletedTask;
    private ConcurrentQueue<PayloadData> _pendingPollReplies = new();

    public Device(ILogger<DeviceProxy> logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Dispose() => Dispose(true);

    public bool IsConnected { get; private set; } = false;

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellationTokenSource?.Dispose();
        }
    }

    public async void StartListening(IOsdpConnection connection)
    {
        var cancellationTokenSource = _cancellationTokenSource;
        if (cancellationTokenSource != null) return;
        _cancellationTokenSource = cancellationTokenSource = new CancellationTokenSource();

        _listenerTask = await Task.Factory.StartNew(async () =>
        {
            try
            {
                connection.Open();

                var channel = new PdMessageSecureChannel(connection);

                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var command = await channel.ReadNextCommand(_cancellationTokenSource.Token);
                    if (command != null)
                    {
                        var reply = HandleCommand(command);
                        channel.SendReply(reply);
                    }
                }

                IsConnected = false;
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception, $"Unexpected exception in polling loop");
            }
        }, TaskCreationOptions.LongRunning);
    }

    public async void StopListening()
    {
        var cancellationTokenSource = _cancellationTokenSource;
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();

            // TODO: why not block indefinitely?
            //_shutdownComplete.WaitOne(TimeSpan.FromSeconds(1));
            await _listenerTask;
            _cancellationTokenSource = null;
        }
    }

    public void EnqueuePollReply(PayloadData reply) => _pendingPollReplies.Enqueue(reply);

    protected virtual OutgoingMessage HandleCommand(IncomingMessage command) =>
        new(command.ControlBlock, (CommandType)command.Type switch
        {
            CommandType.Poll => HandlePoll(),
            CommandType.IdReport => HandleIdReport(),
            CommandType.DeviceCapabilities => HandleDeviceCap(command),
            CommandType.LocalStatus => HandleLocalStatusReport(command),
            CommandType.InputStatus => HandleInputStatusReport(command),
            CommandType.OutputStatus => HandleOutputStatusReport(command),
            CommandType.ReaderStatus => HandleReaderStatusReport(command),
            CommandType.OutputControl => HandleOutputControl(command),
            CommandType.LEDControl => HandleReaderLEDControl(command),
            CommandType.BuzzerControl => HandleBuzzerControl(command),
            CommandType.TextOutput => HandleTextOutput(command),
            CommandType.CommunicationSet => HandleCommunicationSet(command),
            CommandType.BioRead => HandleBiometricRead(command),
            CommandType.BioMatch => HandleBiometricMatch(command),
            CommandType.KeySet => HandleKeySettings(command),
            CommandType.SessionChallenge => HandleSessionChallenge(command),
            CommandType.ServerCryptogram => HandleServerCryptogram(command),
            CommandType.MaxReplySize => HandleMaxReplySize(command),
            CommandType.FileTransfer => HandleFileTransfer(command),
            CommandType.ManufacturerSpecific => HandleManufacturerCommand(command),
            CommandType.Abort => HandleAbortRequest(command),
            CommandType.PivData => HandlePivData(command),
            CommandType.KeepActive => HandleKeepActive(command),
            _ => HandleUnknownCommand(command),
        });

    protected virtual PayloadData HandlePoll()
    {
        return _pendingPollReplies.TryDequeue(out var reply) ? reply : new Ack();
    }

    protected virtual PayloadData HandleIdReport()
    {
        return HandleUnknownCommand(CommandType.IdReport);
    }

    protected virtual PayloadData HandleTextOutput(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleBuzzerControl(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleOutputControl(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleDeviceCap(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandlePivData(IncomingMessage command)
    {
        var payload = GetPIVData.ParseData(command.Payload);

        // TODO: This is where we would trigger async gathering of PIV data which
        // will be returned through a reply to a future poll command

        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleManufacturerCommand(IncomingMessage command)
    {
        var payload = Model.CommandData.ManufacturerSpecific.ParseData(command.Payload);

        // TODO: This is where we would trigger async manufacturer-specific command
        // reply to which would be returned as a reply to a future poll command

        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleKeepActive(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleAbortRequest(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleFileTransfer(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleMaxReplySize(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleSessionChallenge(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleServerCryptogram(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleKeySettings(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleBiometricMatch(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleBiometricRead(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleCommunicationSet(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleReaderLEDControl(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleReaderStatusReport(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleOutputStatusReport(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleInputStatusReport(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleLocalStatusReport(IncomingMessage command)
    {
        return HandleUnknownCommand(command);
    }

    protected virtual PayloadData HandleUnknownCommand(IncomingMessage command)
    {
        byte[] rawMessage = command.OriginalMessageData.ToArray();

        _logger.LogInformation($"Unexpected Command: {{cmd_bytes}}!!{Environment.NewLine}" +
            $"    Cmd: {{cmd_code}}({{cmd_name}}){Environment.NewLine}" +
            $"    Payload: {{payload}}",
            string.Join("-", rawMessage.Select(x => x.ToString("X2"))),
            command.Type, Enum.GetName(typeof(CommandType), command.Type),
            string.Join("-", command.Payload.Select(x => x.ToString("X2"))));

        return new Nak(ErrorCode.UnknownCommandCode);
    }

    protected virtual PayloadData HandleUnknownCommand(CommandType commandType)
    {
        _logger.LogInformation($"Unexpected Command: {Environment.NewLine}" +
            $"    Cmd: {{cmd_code}}({{cmd_name}})",
            (int)commandType, Enum.GetName(typeof(CommandType), commandType));

        return new Nak(ErrorCode.UnknownCommandCode);
    }
}
