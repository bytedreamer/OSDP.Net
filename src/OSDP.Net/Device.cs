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
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<PayloadData> _pendingPollReplies = new();

    private volatile int _connectionContextCounter;
    private DeviceConfiguration _deviceConfiguration;
    private IOsdpServer _osdpServer;
    private DateTime _lastValidReceivedCommand = DateTime.MinValue;

    /// <summary>
    /// Represents a Peripheral Device (PD) that communicates over the OSDP protocol.
    /// </summary>
    public Device(DeviceConfiguration config, ILoggerFactory loggerFactory = null)
    {
        _deviceConfiguration = config;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<Device>();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets a value indicating whether the device is currently connected.
    /// </summary>
    /// <value><c>true</c> if the device is connected; otherwise, <c>false</c>.</value>
    public bool IsConnected => _osdpServer?.ConnectionCount > 0 && (
        _lastValidReceivedCommand + TimeSpan.FromSeconds(8) >= DateTime.UtcNow);

    /// <summary>
    /// Disposes the Device instance.
    /// </summary>
    /// <remarks>
    /// This method is responsible for releasing any resources used by the Device instance. 
    /// </remarks>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            var _ = StopListening();
        }
    }

    /// <summary>
    /// Starts listening for commands from the OSDP device through the specified connection.
    /// </summary>
    /// <param name="server">The I/O server used for communication with the OSDP client.</param>
    public async void StartListening(IOsdpServer server)
    {
        _osdpServer = server ?? throw new ArgumentNullException(nameof(server));
        await _osdpServer.Start(ClientListenLoop);
    }

    private async Task ClientListenLoop(IOsdpConnection incomingConnection)
    {
        try
        {
            var currentContextCount = _connectionContextCounter;
            var channel = new PdMessageSecureChannel(
                incomingConnection, _deviceConfiguration.SecurityKey, loggerFactory: _loggerFactory);
            channel.DefaultKeyAllowed = _deviceConfiguration.DefaultSecurityKeyAllowed;

            while (incomingConnection.IsOpen)
            {
                var command = await channel.ReadNextCommand();

                if (command == null) continue;

                var reply = HandleCommand(command);
                await channel.SendReply(reply);

                if (currentContextCount != _connectionContextCounter)
                {
                    _logger?.LogInformation("Interruping existing connection due to 'force disconnect' flag");
                    break;
                }
            }
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception, $"Unexpected exception in polling loop");
        }
        finally
        {
            await incomingConnection.Close();
        }
    }

    /// <summary>
    /// Stops listening for OSDP messages on the device.
    /// </summary>
    public async Task StopListening()
    {
        await (_osdpServer?.Stop() ?? Task.CompletedTask);
        _osdpServer = null;
    }

    /// <summary>
    /// Enqueues a reply into the pending poll reply queue.
    /// </summary>
    /// <param name="reply">The reply to enqueue.</param>
    public void EnqueuePollReply(PayloadData reply) => _pendingPollReplies.Enqueue(reply);

    internal virtual OutgoingReply HandleCommand(IncomingMessage command)
    {
        if (command.IsDataCorrect && Enum.IsDefined(typeof(CommandType), command.Type))
            _lastValidReceivedCommand = DateTime.UtcNow;

        return new OutgoingReply(command, (CommandType)command.Type switch
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
            CommandType.KeySet => _HandleKeySettings(EncryptionKeyConfiguration.ParseData(command.Payload)),
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

    private PayloadData _HandleKeySettings(EncryptionKeyConfiguration commandPayload)
    {
        var response = HandleKeySettings(commandPayload);

        if (response.Code == (byte)ReplyType.Ack)
        {
            UpdateDeviceConfig(c => c.SecurityKey = commandPayload.KeyData);
        }

        return response;
    }

    /// <summary>
    /// If deriving PD class is intending to support secure connections, it MUST override
    /// this method in order to provide its own means of persisting a newly set security key which
    /// which was sent by the ACU. The base `Device` class will automatically pick up the new key
    /// for future connections if this function returns successful Ack response.
    /// NOTE: Any existing connections will continue to use the previous key. It is up to the
    /// ACU to drop connection and reconnect if it wishes to do so
    /// </summary>
    /// <param name="commandPayload">The key settings command payload.</param>
    /// <returns>
    /// Ack - if the new key was successfully accepted
    /// Nak - if the new key was rejected
    /// </returns>
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

    private void UpdateDeviceConfig(Action<DeviceConfiguration> updateAction, bool resetConnection = false)
    {
        var configCopy = _deviceConfiguration.Clone();
        updateAction(configCopy);
        _deviceConfiguration = configCopy;

        if (resetConnection)
        {
            Interlocked.Add(ref _connectionContextCounter, 1);
        }
    }
}


/// <summary>
/// Represents a set of configuration options to be used when initializating 
/// a new instance of the Device class
/// </summary>
public class DeviceConfiguration : ICloneable
{
    /// <summary>
    /// Address the device is assigned 
    /// </summary>
    public byte Address { get; set; } = 0;

    /// <summary>
    /// As described in D.8: Field Deployment and Configuration, this flag enables
    /// "installation mode" which will allow SCBK-D (i.e. default security key) to be
    /// accepted even if a different key has already been set on the device. This flag
    /// should be used during setup and NOT for the operation of the device
    /// </summary>
    public bool DefaultSecurityKeyAllowed { get; set; } = false;

    /// <summary>
    /// Security Key if one was previously set via osdp_KeySet command or some
    /// other out-of-band means
    /// </summary>
    public byte[] SecurityKey { get; set; } = SecurityContext.DefaultKey;

    /// <summary>
    /// Creates a new object that is a copy of the current instance
    /// </summary>
    public DeviceConfiguration Clone() => (DeviceConfiguration)this.MemberwiseClone();

    /// <inheritdoc/>
    object ICloneable.Clone() => this.Clone();
}