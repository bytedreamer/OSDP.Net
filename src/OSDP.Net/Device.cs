using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OSDP.Net.Connections;
using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Model.ReplyData;
using CommunicationConfiguration = OSDP.Net.Model.CommandData.CommunicationConfiguration;
using ManufacturerSpecific = OSDP.Net.Model.CommandData.ManufacturerSpecific;

namespace OSDP.Net;

/// <summary>
/// Represents a Peripheral Device (PD) that communicates over the OSDP protocol.
/// </summary>
public class Device : IDisposable
{
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<PayloadData> _pendingPollReplies = new();

    private CancellationTokenSource _cancellationTokenSource;
    private Task _listenerTask = Task.CompletedTask;
    private DateTime _lastValidReceivedCommand = DateTime.MinValue;
    private bool _isDeviceListening;

    /// <summary>
    /// Represents a Peripheral Device (PD) that communicates over the OSDP protocol.
    /// </summary>
    public Device(ILogger<Device> logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether the device is currently connected.
    /// </summary>
    /// <value><c>true</c> if the device is connected; otherwise, <c>false</c>.</value>
    public bool IsConnected =>
        _lastValidReceivedCommand + TimeSpan.FromSeconds(8) >= DateTime.UtcNow && _isDeviceListening;

    /// <summary>
    /// Starts listening for commands from the OSDP device through the specified connection.
    /// </summary>
    /// <param name="connection">The I/O connection used for communication with the OSDP device.</param>
    public async void StartListening(IOsdpConnection connection)
    {
        _cancellationTokenSource = new CancellationTokenSource();

        _listenerTask = await Task.Factory.StartNew(async () =>
        {
            try
            {
                connection.Open();
                var channel = new PdMessageSecureChannel(connection);
                _isDeviceListening = true;

                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var command = await channel.ReadNextCommand(_cancellationTokenSource.Token);
                    if (command != null)
                    {
                        var reply = HandleCommand(command);
                        await channel.SendReply(reply);
                    }
                }

                _isDeviceListening = false;
            }
            catch (Exception exception)
            {
                _isDeviceListening = false;
                _logger?.LogError(exception, $"Unexpected exception in polling loop");
            }
        }, TaskCreationOptions.LongRunning);
    }

    /// <summary>
    /// Stops listening for OSDP messages on the device.
    /// </summary>
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
            _isDeviceListening = false;
        }
    }

    /// <summary>
    /// Enqueues a reply into the pending poll reply queue.
    /// </summary>
    /// <param name="reply">The reply to enqueue.</param>
    public void EnqueuePollReply(PayloadData reply) => _pendingPollReplies.Enqueue(reply);

    internal virtual OutgoingMessage HandleCommand(IncomingMessage command)
    {
        if (command.IsDataCorrect && Enum.IsDefined(typeof(CommandType), command.Type))
            _lastValidReceivedCommand = DateTime.UtcNow;

        return new OutgoingMessage((byte)(command.Address | 0x80), command.ControlBlock,
            (CommandType)command.Type switch
            {
                CommandType.Poll => HandlePoll(),
                CommandType.IdReport => HandleIdReport(),
                CommandType.DeviceCapabilities => HandleDeviceCapabilities(),
                CommandType.LocalStatus => HandleLocalStatusReport(),
                CommandType.InputStatus => HandleInputStatusReport(),
                CommandType.OutputStatus => HandleOutputStatusReport(),
                CommandType.ReaderStatus => HandleReaderStatusReport(),
                CommandType.OutputControl => HandleOutputControl(OutputControls.ParseData(command.Payload)),
                CommandType.LEDControl => HandleReaderLEDControl(ReaderLedControls.ParseData(command.Payload)),
                CommandType.BuzzerControl => HandleBuzzerControl(ReaderBuzzerControl.ParseData(command.Payload)),
                CommandType.TextOutput => HandleTextOutput(ReaderTextOutput.ParseData(command.Payload)),
                CommandType.CommunicationSet => HandleCommunicationSet(CommunicationConfiguration.ParseData(command.Payload)),
                CommandType.BioRead => HandleBiometricRead(BiometricReadData.ParseData(command.Payload)),
                CommandType.BioMatch => HandleBiometricMatch(BiometricTemplateData.ParseData(command.Payload)),
                CommandType.KeySet => HandleKeySettings(EncryptionKeyConfiguration.ParseData(command.Payload)),
                CommandType.SessionChallenge => HandleSessionChallenge(),
                CommandType.ServerCryptogram => HandleServerCryptogram(),
                CommandType.MaxReplySize => HandleMaxReplySize(ACUReceiveSize.ParseData(command.Payload)),
                CommandType.FileTransfer => HandleFileTransfer(FileTransferFragment.ParseData(command.Payload)),
                CommandType.ManufacturerSpecific => HandleManufacturerCommand(ManufacturerSpecific.ParseData(command.Payload)),
                CommandType.Abort => HandleAbortRequest(),
                CommandType.PivData => HandlePivData(GetPIVData.ParseData(command.Payload)),
                CommandType.KeepActive => HandleKeepActive(KeepReaderActive.ParseData(command.Payload)),
                _ => HandleUnknownCommand(command)
            });
    }

    private PayloadData HandlePoll()
    {
        return _pendingPollReplies.TryDequeue(out var reply) ? reply : new Ack();
    }

    /// <summary>
    /// Handles the ID Report Request command received from the OSDP device.
    /// </summary>
    /// <returns></returns>
    protected virtual PayloadData HandleIdReport()
    {
        return HandleUnknownCommand(CommandType.IdReport);
    }

    /// <summary>
    /// Handles the text output command received from the OSDP device.
    /// </summary>
    /// <param name="commandPayload">The incoming reader text output command payload.</param>
    /// <returns></returns>
    protected virtual PayloadData HandleTextOutput(ReaderTextOutput commandPayload)
    {
        return HandleUnknownCommand(CommandType.TextOutput);
    }

    /// <summary>
    /// Handles the reader buzzer control command received from the OSDP device.
    /// </summary>
    /// <param name="commandPayload">The incoming reader buzzer control command payload.</param>
    /// <returns></returns>
    protected virtual PayloadData HandleBuzzerControl(ReaderBuzzerControl commandPayload)
    {
        return HandleUnknownCommand(CommandType.BuzzerControl);
    }

    /// <summary>
    /// Handles the output controls command received from the OSDP device.
    /// </summary>
    /// <param name="commandPayload">The incoming output controls command payload.</param>
    /// <returns></returns>
    protected virtual PayloadData HandleOutputControl(OutputControls commandPayload)
    {
        return HandleUnknownCommand(CommandType.OutputControl);
    }

    /// <summary>
    /// Handles the output control command received from the OSDP device.
    /// </summary>
    /// <returns></returns>
    protected virtual PayloadData HandleDeviceCapabilities()
    {
        return HandleUnknownCommand(CommandType.DeviceCapabilities);
    }

    /// <summary>
    /// Handles the get PIV data command received from the OSDP device.
    /// </summary>
    /// <param name="commandPayload">The incoming get PIV data command payload.</param>
    /// <returns></returns>
    protected virtual PayloadData HandlePivData(GetPIVData commandPayload)
    {
        return HandleUnknownCommand(CommandType.PivData);
    }

    /// <summary>
    /// Handles the manufacture command received from the OSDP device.
    /// </summary>
    /// <param name="commandPayload">The incoming manufacture command payload.</param>
    /// <returns></returns>
    protected virtual PayloadData HandleManufacturerCommand(ManufacturerSpecific commandPayload)
    {
        return HandleUnknownCommand(CommandType.ManufacturerSpecific);
    }

    /// <summary>
    /// Handles the keep active command received from the OSDP device.
    /// </summary>
    /// <param name="commandPayload">The incoming keep active command payload.</param>
    /// <returns></returns>
    protected virtual PayloadData HandleKeepActive(KeepReaderActive commandPayload)
    {
        return HandleUnknownCommand(CommandType.KeepActive);
    }

    /// <summary>
    /// Handles the abort request command received from the OSDP device.
    /// </summary>
    /// <returns></returns>
    protected virtual PayloadData HandleAbortRequest()
    {
        return HandleUnknownCommand(CommandType.Abort);
    }

    /// <summary>
    /// Handles the file transfer command received from the OSDP device.
    /// </summary>
    /// <param name="commandPayload">The incoming file transfer fragment command message.</param>
    /// <returns></returns>
    private PayloadData HandleFileTransfer(FileTransferFragment commandPayload)
    {
        _logger.LogInformation("Received a file transfer command: {CommandPayload}", commandPayload.ToString());
        return HandleUnknownCommand(CommandType.FileTransfer);
    }

    /// <summary>
    /// Handles the maximum ACU maximum receive size command received from the OSDP device.
    /// </summary>
    /// <param name="commandPayload">The ACU maximum receive size command payload.</param>
    /// <returns></returns>
    protected virtual PayloadData HandleMaxReplySize(ACUReceiveSize commandPayload)
    {
        return HandleUnknownCommand(CommandType.MaxReplySize);
    }

    private PayloadData HandleSessionChallenge()
    {
        _logger.LogInformation("Received a session challenge command");
        return HandleUnknownCommand(CommandType.SessionChallenge);
    }

    private PayloadData HandleServerCryptogram()
    {
        _logger.LogInformation("Received a server cryptogram command");
        return HandleUnknownCommand(CommandType.ServerCryptogram);
    }

    /// <summary>
    /// Handles the key settings command received from the OSDP device.
    /// </summary>
    /// <param name="commandPayload">The key settings command payload.</param>
    /// <returns></returns>
    protected virtual PayloadData HandleKeySettings(EncryptionKeyConfiguration commandPayload)
    {
        return HandleUnknownCommand(CommandType.KeySet);
    }

    /// <summary>
    /// Handles the biometric match command received from the OSDP device.
    /// </summary>
    /// <param name="commandPayload">The biometric match command payload.</param>
    /// <returns></returns>
    protected virtual PayloadData HandleBiometricMatch(BiometricTemplateData commandPayload)
    {
        return HandleUnknownCommand(CommandType.BioMatch);
    }

    /// <summary>
    /// Handles the biometric match command received from the OSDP device.
    /// </summary>
    /// <param name="commandPayload">The biometric match command payload.</param>
    /// <returns></returns>
    protected virtual PayloadData HandleBiometricRead(BiometricReadData commandPayload)
    {
        return HandleUnknownCommand(CommandType.BioRead);
    }

    /// <summary>
    /// Handles the communication set command received from the OSDP device.
    /// </summary>
    /// <param name="commandPayload">The communication set command payload.</param>
    /// <returns></returns>
    protected virtual PayloadData HandleCommunicationSet(CommunicationConfiguration commandPayload)
    {
        return HandleUnknownCommand(CommandType.CommunicationSet);
    }

    /// <summary>
    /// Handles the reader LED controls command received from the OSDP device.
    /// </summary>
    /// <param name="commandPayload">The reader LED controls command payload.</param>
    /// <returns></returns>
    protected virtual PayloadData HandleReaderLEDControl(ReaderLedControls commandPayload)
    {
        return HandleUnknownCommand(CommandType.LEDControl);
    }

    /// <summary>
    /// Handles the reader status command received from the OSDP device.
    /// </summary>
    /// <returns></returns>
    protected virtual PayloadData HandleReaderStatusReport()
    {
        return HandleUnknownCommand(CommandType.ReaderStatus);
    }

    /// <summary>
    /// Handles the output status command received from the OSDP device.
    /// </summary>
    /// <returns></returns>
    protected virtual PayloadData HandleOutputStatusReport()
    {
        return HandleUnknownCommand(CommandType.OutputStatus);
    }

    /// <summary>
    /// Handles the input status command received from the OSDP device.
    /// </summary>
    /// <returns></returns>
    protected virtual PayloadData HandleInputStatusReport()
    {
        return HandleUnknownCommand(CommandType.InputStatus);
    }

    /// <summary>
    /// Handles the reader local status command received from the OSDP device.
    /// </summary>
    /// <returns></returns>
    protected virtual PayloadData HandleLocalStatusReport()
    {
        return HandleUnknownCommand(CommandType.LocalStatus);
    }

    private PayloadData HandleUnknownCommand(IncomingMessage command)
    {
        _logger?.LogInformation("Unexpected Command: {CommandType}", (CommandType)command.Type);

        return new Nak(ErrorCode.UnknownCommandCode);
    }

   private PayloadData HandleUnknownCommand(CommandType commandType)
    {
        _logger?.LogInformation("Unexpected Command: {CommandType}", commandType);

        return new Nak(ErrorCode.UnknownCommandCode);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        _listenerTask?.Dispose();
    }
}
