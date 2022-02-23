using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OSDP.Net.Connections;
using OSDP.Net.Messages;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Model.ReplyData;
using OSDP.Net.Tracing;
using CommunicationConfiguration = OSDP.Net.Model.CommandData.CommunicationConfiguration;
using ManufacturerSpecific = OSDP.Net.Model.ReplyData.ManufacturerSpecific;

namespace OSDP.Net
{
    /// <summary>The OSDP control panel used to communicate to Peripheral Devices (PDs) as an Access Control Unit (ACU). If multiple connections are needed, add them to the control panel. Avoid creating multiple control panel objects.</summary>
    public class ControlPanel
    {
        private readonly ConcurrentDictionary<Guid, Bus> _buses = new ConcurrentDictionary<Guid, Bus>();
        private readonly ILogger<ControlPanel> _logger;
        private readonly BlockingCollection<Reply> _replies = new BlockingCollection<Reply>();
        private readonly TimeSpan _replyResponseTimeout = TimeSpan.FromSeconds(8);
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _requestLocks = new ConcurrentDictionary<int, SemaphoreSlim>();


        /// <summary>Initializes a new instance of the <see cref="T:OSDP.Net.ControlPanel" /> class.</summary>
        /// <param name="logger">The logger definition used for logging.</param>
        public ControlPanel(ILogger<ControlPanel> logger = null)
        {
            _logger = logger;
            
            Task.Factory.StartNew(() =>
            {
                foreach (var reply in _replies.GetConsumingEnumerable())
                {
                    // _logger?.LogDebug($"Received a reply {reply}");
                    
                    OnReplyReceived(reply);
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Start polling on the defined connection.
        /// </summary>
        /// <param name="connection">This represents the type of connection used for communicating to PDs.</param>
        /// <returns>An identifier that represents the connection</returns>
        public Guid StartConnection(IOsdpConnection connection) => StartConnection(connection, Bus.DefaultPollInterval);

        /// <summary>
        /// Start polling on the defined connection.
        /// </summary>
        /// <param name="connection">This represents the type of connection used for communicating to PDs.</param>
        /// <param name="pollInterval">The interval at which the devices will be polled, zero or less indicates no polling</param>
        /// <param name="isTracing">Write packet data to {Bus ID}.osdpcap file</param>
        /// <returns>An identifier that represents the connection</returns>
        public Guid StartConnection(IOsdpConnection connection, TimeSpan pollInterval, bool isTracing) =>
            StartConnection(connection, pollInterval, isTracing ? OSDPFileCapTracer.Trace : _ => { });

        /// <summary>
        /// Start polling on the defined connection.
        /// </summary>
        /// <param name="connection">This represents the type of connection used for communicating to PDs.</param>
        /// <param name="pollInterval">The interval at which the devices will be polled, zero or less indicates no polling</param>
        /// <returns>An identifier that represents the connection</returns>
        public Guid StartConnection(IOsdpConnection connection, TimeSpan pollInterval) =>
            StartConnection(connection, pollInterval, _ => { });

        /// <summary>
        /// Start polling on the defined connection.
        /// </summary>
        /// <param name="connection">This represents the type of connection used for communicating to PDs.</param>
        /// <param name="pollInterval">The interval at which the devices will be polled, zero or less indicates no polling</param>
        /// <param name="tracer">Delegate that will receive detailed trace information</param>
        /// <returns>An identifier that represents the connection</returns>
        public Guid StartConnection(IOsdpConnection connection, TimeSpan pollInterval, Action<TraceEntry> tracer)
        {
            var newBus = new Bus(
                connection,
                _replies,
                pollInterval,
                tracer,
                _logger);

            newBus.ConnectionStatusChanged += BusOnConnectionStatusChanged;

            _buses[newBus.Id] = newBus;

            Task.Factory.StartNew(async () =>
            {
                await newBus.StartPollingAsync().ConfigureAwait(false);
            }, TaskCreationOptions.LongRunning);

            return newBus.Id;
        }

        /// <summary>
        /// Stop the bus for a specific connection.
        /// </summary>
        /// <param name="connectionId">The identifier that represents the connection.</param>
        public void StopConnection(Guid connectionId)
        {
            if (!_buses.TryRemove(connectionId, out Bus bus))
            {
                return;
            }

            bus.ConnectionStatusChanged -= BusOnConnectionStatusChanged;
            bus.Close();

            foreach (byte address in bus.ConfigureDeviceAddresses)
            {
                OnConnectionStatusChanged(bus.Id, address, false, false);
            }
            bus.Dispose();
        }

        private void BusOnConnectionStatusChanged(object sender, Bus.ConnectionStatusEventArgs eventArgs)
        {
            if (sender is Bus bus)
                OnConnectionStatusChanged(bus.Id, eventArgs.Address, eventArgs.IsConnected,
                    eventArgs.IsSecureSessionEstablished);
        }

        /// <summary>
        /// Send a custom command for testing.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="command">The custom command to send.</param>
        public async Task SendCustomCommand(Guid connectionId, Command command)
        {
            await SendCommand(connectionId, command).ConfigureAwait(false);
        }

        /// <summary>Request to get an ID Report from the PD.</summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <returns>ID report reply data that was requested.</returns>
        public async Task<DeviceIdentification> IdReport(Guid connectionId, byte address)
        {
            return DeviceIdentification.ParseData((await SendCommand(connectionId,
                new IdReportCommand(address)).ConfigureAwait(false)).ExtractReplyData);
        }

        /// <summary>Request to get the capabilities of the PD.</summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <returns>Device capabilities reply data that was requested.</returns>
        public async Task<DeviceCapabilities> DeviceCapabilities(Guid connectionId, byte address)
        {
            return Model.ReplyData.DeviceCapabilities.ParseData((await SendCommand(connectionId,
                new DeviceCapabilitiesCommand(address)).ConfigureAwait(false)).ExtractReplyData);
        }

        /// <summary>
        /// Command that implements extended write mode to facilitate communications with an ISO 7816-4 based credential to a PD.
        /// </summary>
        /// <summary>Request to get the capabilities of the PD.</summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="extendedWrite">The extended write data.</param>
        /// <returns>Reply data that is returned after sending the command. There is the possibility of different replies can be returned from PD.</returns>
        public async Task<ReturnReplyData<ExtendedRead>> ExtendedWriteData(Guid connectionId, byte address,
            ExtendedWrite extendedWrite)
        {
            var reply = await SendCommand(connectionId,
                new ExtendedWriteDataCommand(address, extendedWrite)).ConfigureAwait(false);

            return new ReturnReplyData<ExtendedRead>
            {
                Ack = reply.Type == ReplyType.Ack,
                Nak = reply.Type == ReplyType.Nak ? Nak.ParseData(reply.ExtractReplyData) : null,
                ReplyData = reply.Type == ReplyType.ExtendedRead ? ExtendedRead.ParseData(reply.ExtractReplyData) : null
            };
        }

        /// <summary>
        /// Request to get the local status of a PD.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <returns>Device local status reply data that was requested.</returns>
        public async Task<LocalStatus> LocalStatus(Guid connectionId, byte address)
        {
            return Model.ReplyData.LocalStatus.ParseData((await SendCommand(connectionId,
                new LocalStatusReportCommand(address)).ConfigureAwait(false)).ExtractReplyData);
        }

        /// <summary>
        /// Request to get the status all of the inputs of a PD.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <returns>Device input status reply data that was requested.</returns>
        public async Task<InputStatus> InputStatus(Guid connectionId, byte address)
        {
            return Model.ReplyData.InputStatus.ParseData((await SendCommand(connectionId,
                new InputStatusReportCommand(address)).ConfigureAwait(false)).ExtractReplyData);
        }

        /// <summary>
        /// Request to get the status all of the outputs of a PD.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <returns>Device output status reply data that was requested.</returns>
        public async Task<OutputStatus> OutputStatus(Guid connectionId, byte address)
        {
            return Model.ReplyData.OutputStatus.ParseData((await SendCommand(connectionId,
                new OutputStatusReportCommand(address)).ConfigureAwait(false)).ExtractReplyData);
        }

        /// <summary>
        /// Request to get PIV data from PD. Only one PIV request is processed at a time for each connection. 
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="getPIVData">Describe the PIV data to retrieve.</param>
        /// <param name="timeout">A TimeSpan that represents time to wait until waiting for the other requests to complete and it's own request, a TimeSpan that represents -1 milliseconds to wait indefinitely, or a TimeSpan that represents 0 milliseconds to test the wait handle and return immediately.</param>
        /// <param name="cancellationToken">The CancellationToken token to observe.</param>
        /// <returns>A response with the PIV data requested.</returns>
        public async Task<byte[]> GetPIVData(Guid connectionId, byte address, GetPIVData getPIVData, TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var requestLock = GetRequestLock(connectionId, address);
            
            if (!await requestLock.WaitAsync(timeout, cancellationToken))
            {
                throw new TimeoutException("Timeout waiting for another request to complete.");
            }
            
            try
            {
                return await WaitForPIVData(connectionId, address, getPIVData, timeout, cancellationToken);
            }
            finally
            {
                requestLock.Release();
            }
        }

        private SemaphoreSlim GetRequestLock(Guid connectionId, byte address)
        {
            int hash = new { connectionId, address }.GetHashCode();
            
            if (_requestLocks.TryGetValue(hash, out var requestLock))
            {
                return requestLock;
            }

            var newRequestLock = new SemaphoreSlim(1, 1);
            _requestLocks[hash] = newRequestLock;
            return newRequestLock;
        }

        private async Task<byte[]> WaitForPIVData(Guid connectionId, byte address, GetPIVData getPIVData, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            bool complete = false;
            DateTime endTime = DateTime.UtcNow + timeout;
            byte[] data = null;

            void Handler(object sender, MultiPartMessageDataReplyEventArgs args)
            {
                // Only process matching replies
                if (args.ConnectionId != connectionId || args.Address != address) return;

                var pivData = args.DataFragmentResponse;
                data ??= new byte[pivData.WholeMessageLength];

                complete = Message.BuildMultiPartMessageData(pivData.WholeMessageLength, pivData.Offset,
                    pivData.LengthOfFragment, pivData.Data, data);
            }

            PIVDataReplyReceived += Handler;

            try
            {
                await SendCommand(connectionId,
                    new GetPIVDataCommand(address, getPIVData), cancellationToken).ConfigureAwait(false);

                while (DateTime.UtcNow <= endTime)
                {
                    if (complete)
                    {
                        return data;
                    }

                    // Delay for default poll interval
                    await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
                }

                throw new TimeoutException("Timeout waiting to receive PIV data.");
            }
            finally
            {
                PIVDataReplyReceived -= Handler;
            }
        }

        /// <summary>
        /// Request to get the status all of the readers of a PD.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <returns>Device reader status reply data that was requested.</returns>
        public async Task<ReaderStatus> ReaderStatus(Guid connectionId, byte address)
        {
            return Model.ReplyData.ReaderStatus.ParseData((await SendCommand(connectionId,
                new ReaderStatusReportCommand(address)).ConfigureAwait(false)).ExtractReplyData);
        }

        /// <summary>
        /// Send a  manufacture specific command to a PD.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="manufacturerSpecific">The manufacturer specific data.</param>
        /// <returns>Reply data that is returned after sending the command. There is the possibility of different replies can be returned from PD.</returns>
        public async Task<ReturnReplyData<ManufacturerSpecific>> ManufacturerSpecificCommand(Guid connectionId, byte address,
            Model.CommandData.ManufacturerSpecific manufacturerSpecific)
        {
            var reply = await SendCommand(connectionId,
                new ManufacturerSpecificCommand(address, manufacturerSpecific)).ConfigureAwait(false);

            return new ReturnReplyData<ManufacturerSpecific>
            {
                Ack = reply.Type == ReplyType.Ack,
                Nak = reply.Type == ReplyType.Nak ? Nak.ParseData(reply.ExtractReplyData) : null,
                ReplyData = reply.Type == ReplyType.ManufactureSpecific ? ManufacturerSpecific.ParseData(reply.ExtractReplyData) : null
            };
        }

        /// <summary>
        /// Send a command to alter the state of one or more outputs on a PD.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="outputControls">Data for one or more outputs to control.</param>
        /// <returns>Reply data that is returned after sending the command. There is the possibility of different replies can be returned from PD.</returns>
        public async Task<ReturnReplyData<OutputStatus>> OutputControl(Guid connectionId, byte address, OutputControls outputControls)
        {
            var reply = await SendCommand(connectionId,
                new OutputControlCommand(address, outputControls)).ConfigureAwait(false);
            
            return new ReturnReplyData<OutputStatus>
            {
                Ack = reply.Type == ReplyType.Ack,
                Nak = reply.Type == ReplyType.Nak ? Nak.ParseData(reply.ExtractReplyData) : null,
                ReplyData = reply.Type == ReplyType.OutputStatusReport ? Model.ReplyData.OutputStatus.ParseData(reply.ExtractReplyData) : null
            };
        }

        /// <summary>
        /// Send a command to alter the LED color on a PD.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="readerLedControls">Data to change color on one or more reader LEDs.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public async Task<bool> ReaderLedControl(Guid connectionId, byte address, ReaderLedControls readerLedControls)
        {
            var reply = await SendCommand(connectionId,
                new ReaderLedControlCommand(address, readerLedControls)).ConfigureAwait(false);
            
            return reply.Type == ReplyType.Ack;
        }

        /// <summary>
        /// Send a command to control the buzzer on a PD.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="readerBuzzerControl">Data for the reader buzzer control on the PD.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public async Task<bool> ReaderBuzzerControl(Guid connectionId, byte address, ReaderBuzzerControl readerBuzzerControl)
        {
            var reply = await SendCommand(connectionId,
                new ReaderBuzzerControlCommand(address, readerBuzzerControl)).ConfigureAwait(false);
            
            return reply.Type == ReplyType.Ack;
        }

        /// <summary>
        /// Send a command that sends text to be shown on a PD.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="readerTextOutput">Data for the reader text output on the PD.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public async Task<bool> ReaderTextOutput(Guid connectionId, byte address, ReaderTextOutput readerTextOutput)
        {
            var reply = await SendCommand(connectionId,
                new ReaderTextOutputCommand(address, readerTextOutput)).ConfigureAwait(false);
            
            return reply.Type == ReplyType.Ack;
        }

        /// <summary>
        /// Send a command that sets the communication configuration on a PD.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="communicationConfiguration">Data for the communication configuration on the PD.</param>
        /// <returns>Reply data of the actual communication configuration being set on the PD.</returns>
        public async Task<Model.ReplyData.CommunicationConfiguration> CommunicationConfiguration(Guid connectionId,
            byte address, CommunicationConfiguration communicationConfiguration)
        {
            return Model.ReplyData.CommunicationConfiguration.ParseData((await SendCommand(connectionId,
                    new CommunicationSetCommand(address, communicationConfiguration)).ConfigureAwait(false))
                .ExtractReplyData);
        }

        /// <summary>
        ///  Send a command that sets the encryption key configuration on a PD.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="encryptionKeyConfiguration">Data for the encryption key configuration on the PD.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public async Task<bool> EncryptionKeySet(Guid connectionId, byte address,
            EncryptionKeyConfiguration encryptionKeyConfiguration)
        {
            var reply = await SendCommand(connectionId,
                new EncryptionKeySetCommand(address, encryptionKeyConfiguration)).ConfigureAwait(false);

            return reply.Type == ReplyType.Ack;
        }

        /// <summary>
        /// Send a file to a PD.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="fileType">File transfer type</param>
        /// <param name="fileData">The complete file data being sent to the PD</param>
        /// <param name="fragmentSize">Initial size of the fragment sent with each packet</param>
        /// <param name="callback">Track the status of the file transfer</param>
        /// <param name="cancellationToken">The CancellationToken token to observe.</param>
        public Task FileTransfer(Guid connectionId, byte address, byte fileType, byte[] fileData, ushort fragmentSize,
            Action<FileTransferStatus> callback, CancellationToken cancellationToken = default)
        {
            return Task.Run(async () =>
            {
                _buses[connectionId].SetSendingMultiMessage(address, true);
                try
                {
                    await SendFileTransferCommands(connectionId, address, fileType, fileData, fragmentSize, callback,
                        cancellationToken);
                }
                finally
                {
                    _buses[connectionId].SetSendingMultiMessage(address, false);
                    _buses[connectionId].SetSendingMultiMessageNoSecureChannel(address, false);
                }
            });
        }

        private async Task SendFileTransferCommands(Guid connectionId, byte address, byte fileType, byte[] fileData,
            ushort fragmentSize, Action<FileTransferStatus> callback, CancellationToken cancellationToken)
        {
            int totalSize = fileData.Length;
            int offset = 0;
            bool continueTransfer = true;

            while (!cancellationToken.IsCancellationRequested && continueTransfer)
            {
                // Get the fragment size if it doesn't exceed the total size
                ushort updatedFragmentSize = (ushort)Math.Min(fragmentSize, totalSize - offset);

                var reply = await SendCommand(connectionId,
                        new FileTransferCommand(address,
                            new FileTransferFragment(fileType, totalSize, offset, updatedFragmentSize,
                                fileData.Skip(offset).Take(updatedFragmentSize).ToArray())), cancellationToken)
                    .ConfigureAwait(false);

                // Update offset
                offset += updatedFragmentSize;

                // Parse the fileTransfer status
                var fileTransferStatus = reply.Type == ReplyType.FileTransferStatus
                    ? Model.ReplyData.FileTransferStatus.ParseData(reply.ExtractReplyData)
                    : null;

                if (fileTransferStatus != null)
                {
                    // Leave secure channel if needed
                    if ((fileTransferStatus.Action & Model.ReplyData.FileTransferStatus.ControlFlags.LeaveSecureChannel) ==
                        Model.ReplyData.FileTransferStatus.ControlFlags.LeaveSecureChannel)
                    {
                        _buses[connectionId].SetSendingMultiMessageNoSecureChannel(address, true);
                    }

                    // Set request delay if specified
                    if (fileTransferStatus is { RequestedDelay: > 0 })
                    {
                        _buses[connectionId].SetRequestDelay(address,
                            DateTime.UtcNow.AddMilliseconds(fileTransferStatus.RequestedDelay));
                    }

                    // Set fragment size if requested
                    if (fileTransferStatus is { UpdateMessageMaximum: > 0 })
                    {
                        fragmentSize = Message.CalculateMaximumMessageSize(fileTransferStatus.UpdateMessageMaximum,
                            reply.IsSecureMessage);
                    }
                }

                callback(new FileTransferStatus(
                    fileTransferStatus?.Detail ?? Model.ReplyData.FileTransferStatus.StatusDetail.UnknownError, offset,
                    reply.Type == ReplyType.Nak ? Nak.ParseData(reply.ExtractReplyData) : null));

                // End transfer is Nak reply is received
                if (reply.Type == ReplyType.Nak || (fileTransferStatus?.Detail ??
                                                    Model.ReplyData.FileTransferStatus.StatusDetail.UnknownError) < 0) return;

                // Determine if we should continue on successful status
                if (fileTransferStatus is not
                    { Detail: Model.ReplyData.FileTransferStatus.StatusDetail.FinishingFileTransfer })
                {
                    continueTransfer = offset < totalSize;
                }
                else
                {
                    fragmentSize = 0;
                }
            }
        }

        /// <summary>
        /// Send a request to the PD to read from a biometric scan and send back the data template.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="biometricReadData">Command data to send a request to the PD to send template data from a biometric scan.</param>
        /// <param name="timeout">A TimeSpan that represents time to wait until waiting for the other requests to complete and it's own request, a TimeSpan that represents -1 milliseconds to wait indefinitely, or a TimeSpan that represents 0 milliseconds to test the wait handle and return immediately.</param>
        /// <param name="cancellationToken">The CancellationToken token to observe.</param>
        /// <returns>Results from matching the biometric scan.</returns>
        public async Task<BiometricReadResult> ScanAndSendBiometricData(Guid connectionId, byte address,
            BiometricReadData biometricReadData, TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var requestLock = GetRequestLock(connectionId, address);

            if (!await requestLock.WaitAsync(timeout, cancellationToken))
            {
                throw new TimeoutException("Timeout waiting for another request to complete.");
            }

            try
            {
                return await WaitForBiometricRead(connectionId, address, biometricReadData, timeout,
                    cancellationToken);
            }
            finally
            {
                requestLock.Release();
            }
        }

        private async Task<BiometricReadResult> WaitForBiometricRead(Guid connectionId, byte address,
            BiometricReadData biometricReadData, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            bool complete = false;
            BiometricReadResult result = null;

            void Handler(object sender, BiometricReadResultsReplyEventArgs args)
            {
                // Only process matching replies
                if (args.ConnectionId != connectionId || args.Address != address) return;

                complete = true;

                result = args.BiometricReadResult;
            }

            BiometricReadResultsReplyReceived += Handler;

            try
            {
                await SendCommand(connectionId,
                    new BiometricReadDataCommand(address, biometricReadData), cancellationToken).ConfigureAwait(false);

                DateTime endTime = DateTime.UtcNow + timeout;

                while (DateTime.UtcNow <= endTime)
                {
                    if (complete)
                    {
                        return result;
                    }

                    // Delay for default poll interval
                    await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
                }

                throw new TimeoutException("Timeout waiting to for biometric read data.");
            }
            finally
            {
                BiometricReadResultsReplyReceived -= Handler;
            }
        }


        /// <summary>
        /// Send a request to the PD to perform a biometric scan and match it to the provided template.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="biometricTemplateData">Command data to send a request to the PD to perform a biometric scan and match.</param>
        /// <param name="timeout">A TimeSpan that represents time to wait until waiting for the other requests to complete and it's own request, a TimeSpan that represents -1 milliseconds to wait indefinitely, or a TimeSpan that represents 0 milliseconds to test the wait handle and return immediately.</param>
        /// <param name="cancellationToken">The CancellationToken token to observe.</param>
        /// <returns>Results from matching the biometric scan.</returns>
        public async Task<BiometricMatchResult> ScanAndMatchBiometricTemplate(Guid connectionId, byte address,
            BiometricTemplateData biometricTemplateData, TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var requestLock = GetRequestLock(connectionId, address);
            
            if (!await requestLock.WaitAsync(timeout, cancellationToken))
            {
                throw new TimeoutException("Timeout waiting for another request to complete.");
            }
            
            try
            {
                return await WaitForBiometricMatch(connectionId, address, biometricTemplateData, timeout, cancellationToken);
            }
            finally
            {
                requestLock.Release();
            }
        }

        private async Task<BiometricMatchResult> WaitForBiometricMatch(Guid connectionId, byte address, BiometricTemplateData biometricTemplateData, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            bool complete = false;
            BiometricMatchResult result = null;

            void Handler(object sender, BiometricMatchReplyEventArgs args)
            {
                // Only process matching replies
                if (args.ConnectionId != connectionId || args.Address != address) return;

                complete = true;

                result = args.BiometricMatchResult;
            }

            BiometricMatchReplyReceived += Handler;

            try
            {
                await SendCommand(connectionId,
                    new BiometricMatchCommand(address, biometricTemplateData), cancellationToken).ConfigureAwait(false);
                
                DateTime endTime = DateTime.UtcNow + timeout;
                
                while (DateTime.UtcNow <= endTime)
                {
                    if (complete)
                    {
                        return result;
                    }

                    // Delay for default poll interval
                    await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
                }

                throw new TimeoutException("Timeout waiting to for biometric match.");
            }
            finally
            {
                BiometricMatchReplyReceived -= Handler;
            }
        }

        /// <summary>
        /// Instruct the PD to perform a challenge/response sequence.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="algorithm"></param>
        /// <param name="key"></param>
        /// <param name="challenge"></param>
        /// <param name="fragmentSize">Size of the fragment sent with each packet</param>
        /// <param name="timeout">A TimeSpan that represents time to wait until waiting for the other requests to complete and it's own request, a TimeSpan that represents -1 milliseconds to wait indefinitely, or a TimeSpan that represents 0 milliseconds to test the wait handle and return immediately.</param>
        /// <param name="cancellationToken">The CancellationToken token to observe.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public async Task<byte[]> AuthenticationChallenge(Guid connectionId, byte address,
            byte algorithm, byte key, byte[] challenge, ushort fragmentSize, TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var requestLock = GetRequestLock(connectionId, address);

            if (!await requestLock.WaitAsync(timeout, cancellationToken))
            {
                throw new TimeoutException("Timeout waiting for another request to complete.");
            }

            try
            {
                return await WaitForChallengeResponse(connectionId, address, algorithm, key, challenge, fragmentSize,
                    timeout, cancellationToken);
            }
            finally
            {
                requestLock.Release();
            }
        }

        private async Task<byte[]> WaitForChallengeResponse(Guid connectionId, byte address, byte algorithm, byte key,
            byte[] challenge, ushort fragmentSize, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            bool complete = false;
            DateTime endTime = DateTime.UtcNow + timeout;
            var requestData = new List<byte> { algorithm, key };
            requestData.AddRange(challenge);
            byte[] responseData = null;

            void Handler(object sender, MultiPartMessageDataReplyEventArgs args)
            {
                // Only process matching replies
                if (args.ConnectionId != connectionId || args.Address != address) return;

                var dataFragmentResponse = args.DataFragmentResponse;
                responseData ??= new byte[dataFragmentResponse.WholeMessageLength];

                complete = Message.BuildMultiPartMessageData(dataFragmentResponse.WholeMessageLength, dataFragmentResponse.Offset,
                    dataFragmentResponse.LengthOfFragment, dataFragmentResponse.Data, responseData);
            }

            AuthenticationChallengeResponseReceived += Handler;
            
            int totalSize = requestData.Count;
            int offset = 0;
            bool continueTransfer = true;

            try
            {
                while (!cancellationToken.IsCancellationRequested && continueTransfer)
                {
                    var reply = await SendCommand(connectionId,
                            new AuthenticationChallengeCommand(address,
                                new AuthenticationChallengeFragment(totalSize, offset, fragmentSize,
                                    requestData.Skip(offset).Take((ushort)Math.Min(fragmentSize, totalSize - offset))
                                        .ToArray())), cancellationToken)
                        .ConfigureAwait(false);

                    offset += fragmentSize;

                    // End transfer is Nak reply is received
                    if (reply.Type == ReplyType.Nak) throw new Exception("Unable to complete challenge request");

                    // Determine if we should continue on successful status
                    continueTransfer = offset < totalSize;
                }

                while (DateTime.UtcNow <= endTime)
                {
                    if (complete)
                    {
                        return responseData;
                    }

                    // Delay for default poll interval
                    await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
                }

                throw new TimeoutException("Timeout waiting to receive challenge response.");
            }
            finally
            {
                AuthenticationChallengeResponseReceived -= Handler;
            }
        }

        /// <summary>
        /// Inform the PD the maximum size that the ACU can receive.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="maximumReceiveSize">The maximum size that the ACU can receive.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public async Task<bool> ACUReceivedSize(Guid connectionId, byte address, ushort maximumReceiveSize)
        {
            var reply = await SendCommand(connectionId,
                new ACUReceiveSizeCommand(address, maximumReceiveSize)).ConfigureAwait(false);

            return reply.Type == ReplyType.Ack;
        }

        /// <summary>
        /// Instructs the PD to maintain communication with the credential for a specified time.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="keepAliveTimeInMilliseconds">The length of time to maintain communication with credential.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public async Task<bool> KeepReaderActive(Guid connectionId, byte address, ushort keepAliveTimeInMilliseconds)
        {
            var reply = await SendCommand(connectionId,
                new KeepReaderActiveCommand(address, keepAliveTimeInMilliseconds)).ConfigureAwait(false);

            return reply.Type == ReplyType.Ack;
        }

        /// <summary>
        /// Instructs the PD to abort the current operation.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public async Task<bool> AbortCurrentOperation(Guid connectionId, byte address)
        {
            var reply = await SendCommand(connectionId, new AbortCurrentOperationCommand(address))
                .ConfigureAwait(false);

            return reply.Type == ReplyType.Ack;
        }

        /// <summary>
        /// Is the PD online
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device</param>
        /// <param name="address">Address assigned to the device</param>
        /// <returns>Returns true if the PD is online</returns>
        public bool IsOnline(Guid connectionId, byte address)
        {
            return _buses[connectionId].IsOnline(address);
        }

        internal async Task<Reply> SendCommand(Guid connectionId, Command command,
            CancellationToken cancellationToken = default)
        {
            var source = new TaskCompletionSource<Reply>();

            void EventHandler(object sender, ReplyEventArgs replyEventArgs)
            {
                if (!replyEventArgs.Reply.MatchIssuingCommand(command)) return;
                ReplyReceived -= EventHandler;
                source.SetResult(replyEventArgs.Reply);
            }

            ReplyReceived += EventHandler;

            if (_buses.TryGetValue(connectionId, out var bus))
            {
                bus.SendCommand(command);
            }

            if (source.Task == await Task.WhenAny(source.Task, Task.Delay(_replyResponseTimeout, cancellationToken))
                .ConfigureAwait(false))
            {
                return await source.Task;
            }

            ReplyReceived -= EventHandler;
            throw new TimeoutException();
        }

        /// <summary>
        /// Shutdown the control panel and stop all communication to PDs.
        /// </summary>
        public void Shutdown()
        {
            foreach (var bus in _buses.Values)
            {
                bus.ConnectionStatusChanged -= BusOnConnectionStatusChanged;
                bus.Close();
                
                foreach (byte address in bus.ConfigureDeviceAddresses)
                {
                    OnConnectionStatusChanged(bus.Id, address, false, false);
                }
                bus.Dispose();
            }
            _buses.Clear();

            foreach (var pivDataLock in _requestLocks.Values)
            {
                pivDataLock.Dispose();
            }
            _requestLocks.Clear();
        }

        /// <summary>
        /// Reset communication sequence with the PD specified.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        public void ResetDevice(Guid connectionId, int address)
        {
            if (_buses.TryGetValue(connectionId, out Bus bus))
            {
                bus.ResetDevice(address);
            }
        }

        /// <summary>
        /// Add a PD to the control panel. This will replace an existing PD that is configured at the same address.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="useCrc">Use CRC for error checking.</param>
        /// <param name="useSecureChannel">Require the device to communicate with a secure channel.</param>
        /// <param name="secureChannelKey">Set the secure channel key, default installation key is used if not specified.</param>
        public void AddDevice(Guid connectionId, byte address, bool useCrc, bool useSecureChannel, byte[] secureChannelKey = null)
        {
            if (!_buses.TryGetValue(connectionId, out Bus foundBus))
            {
                throw new ArgumentException("Connection could not be found", nameof(connectionId));
            }

            if (address > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(address), "Address is out of range, it must between 0 and 127.");
            }

            foundBus.AddDevice(address, useCrc, useSecureChannel, useSecureChannel ? secureChannelKey : null);
        }

        /// <summary>
        /// Remove a PD from the control panel.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        public void RemoveDevice(Guid connectionId, byte address)
        {
            if (_buses.TryGetValue(connectionId, out Bus bus))
            {
                bus.RemoveDevice(address);
            }
        }

        private void OnConnectionStatusChanged(Guid connectionId, byte address, bool isConnected,
            bool isSecureChannelEstablished)
        {
            var handler = ConnectionStatusChanged;
            handler?.Invoke(this,
                new ConnectionStatusEventArgs(connectionId, address, isConnected, isSecureChannelEstablished));
        }

        internal virtual void OnReplyReceived(Reply reply)
        {
            {
                var handler = ReplyReceived;
                handler?.Invoke(this, new ReplyEventArgs {Reply = reply});
            }

            switch (reply.Type)
            {
                case ReplyType.Nak:
                {
                    var handler = NakReplyReceived;
                    handler?.Invoke(this,
                        new NakReplyEventArgs(reply.ConnectionId, reply.Address,
                            Nak.ParseData(reply.ExtractReplyData)));
                    break;
                }
                case ReplyType.LocalStatusReport:
                {
                    var handler = LocalStatusReportReplyReceived;
                    handler?.Invoke(this,
                        new LocalStatusReportReplyEventArgs(reply.ConnectionId, reply.Address,
                            Model.ReplyData.LocalStatus.ParseData(reply.ExtractReplyData)));
                    break;
                }
                case ReplyType.InputStatusReport:
                {
                    var handler = InputStatusReportReplyReceived;
                    handler?.Invoke(this,
                        new InputStatusReportReplyEventArgs(reply.ConnectionId, reply.Address,
                            Model.ReplyData.InputStatus.ParseData(reply.ExtractReplyData)));
                    break;
                }
                case ReplyType.OutputStatusReport:
                {
                    var handler = OutputStatusReportReplyReceived;
                    handler?.Invoke(this,
                        new OutputStatusReportReplyEventArgs(reply.ConnectionId, reply.Address,
                            Model.ReplyData.OutputStatus.ParseData(reply.ExtractReplyData)));
                    break;
                }
                case ReplyType.ReaderStatusReport:
                {
                    var handler = ReaderStatusReportReplyReceived;
                    handler?.Invoke(this,
                        new ReaderStatusReportReplyEventArgs(reply.ConnectionId, reply.Address,
                            Model.ReplyData.ReaderStatus.ParseData(reply.ExtractReplyData)));
                    break;
                }

                case ReplyType.RawReaderData:
                {
                    var handler = RawCardDataReplyReceived;
                    handler?.Invoke(this,
                        new RawCardDataReplyEventArgs(reply.ConnectionId, reply.Address,
                            RawCardData.ParseData(reply.ExtractReplyData)));
                    break;
                }

                case ReplyType.ManufactureSpecific:
                {
                    var handler = ManufacturerSpecificReplyReceived;
                    handler?.Invoke(this,
                        new ManufacturerSpecificReplyEventArgs(reply.ConnectionId, reply.Address,
                            ManufacturerSpecific.ParseData(reply.ExtractReplyData)));
                    break;
                }
                
                case ReplyType.ExtendedRead:
                {
                    var handler = ExtendedReadReplyReceived;
                    handler?.Invoke(this,
                        new ExtendedReadReplyEventArgs(reply.ConnectionId, reply.Address,
                            ExtendedRead.ParseData(reply.ExtractReplyData)));
                    break;
                }
                
                case ReplyType.PIVData:
                {
                    var handler = PIVDataReplyReceived;
                    handler?.Invoke(this,
                        new MultiPartMessageDataReplyEventArgs(reply.ConnectionId, reply.Address,
                            DataFragmentResponse.ParseData(reply.ExtractReplyData)));   
                    break;
                }
                
                case ReplyType.ResponseToChallenge:
                {
                    var handler = AuthenticationChallengeResponseReceived;
                    handler?.Invoke(this,
                        new MultiPartMessageDataReplyEventArgs(reply.ConnectionId, reply.Address,
                            DataFragmentResponse.ParseData(reply.ExtractReplyData)));   
                    break;
                }

                case ReplyType.KeypadData:
                {
                    var handler = KeypadReplyReceived;
                    handler?.Invoke(this,
                        new KeypadReplyEventArgs(reply.ConnectionId, reply.Address,
                            KeypadData.ParseData(reply.ExtractReplyData)));
                    break;
                }
                
                case ReplyType.BiometricData:
                {
                    var handler = BiometricReadResultsReplyReceived;
                    handler?.Invoke(this,
                        new BiometricReadResultsReplyEventArgs(reply.ConnectionId, reply.Address,
                            BiometricReadResult.ParseData(reply.ExtractReplyData)));
                    break;
                }
                
                case ReplyType.BiometricMatchResult:
                {
                    var handler = BiometricMatchReplyReceived;
                    handler?.Invoke(this,
                        new BiometricMatchReplyEventArgs(reply.ConnectionId, reply.Address,
                            BiometricMatchResult.ParseData(reply.ExtractReplyData)));
                    break;
                }
            }
        }

        private event EventHandler<ReplyEventArgs> ReplyReceived;

        /// <summary>
        /// Occurs when connection status changed.
        /// </summary>
        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Occurs when a negative reply is received.
        /// </summary>
        public event EventHandler<NakReplyEventArgs> NakReplyReceived;

        /// <summary>
        /// Occurs when local status report reply is received.
        /// </summary>
        public event EventHandler<LocalStatusReportReplyEventArgs> LocalStatusReportReplyReceived;

        /// <summary>
        /// Occurs when input status report reply is received.
        /// </summary>
        public event EventHandler<InputStatusReportReplyEventArgs> InputStatusReportReplyReceived;

        /// <summary>
        /// Occurs when output status report reply is received.
        /// </summary>
        public event EventHandler<OutputStatusReportReplyEventArgs> OutputStatusReportReplyReceived;

        /// <summary>
        /// Occurs when reader status report reply is received.
        /// </summary>
        public event EventHandler<ReaderStatusReportReplyEventArgs> ReaderStatusReportReplyReceived;

        /// <summary>
        /// Occurs when raw card data reply is received.
        /// </summary>
        public event EventHandler<RawCardDataReplyEventArgs> RawCardDataReplyReceived;

        /// <summary>
        /// Occurs when manufacturer specific reply is received.
        /// </summary>
        public event EventHandler<ManufacturerSpecificReplyEventArgs> ManufacturerSpecificReplyReceived;

        /// <summary>
        /// Occurs when extended read reply is received.
        /// </summary>
        public event EventHandler<ExtendedReadReplyEventArgs> ExtendedReadReplyReceived;

        /// <summary>
        /// Occurs when key pad data reply is received.
        /// </summary>
        public event EventHandler<KeypadReplyEventArgs> KeypadReplyReceived;

        /// <summary>
        /// Occurs when biometric read results reply is received.
        /// </summary>
        private event EventHandler<BiometricReadResultsReplyEventArgs> BiometricReadResultsReplyReceived;

        /// <summary>
        /// Occurs when biometric match reply is received.
        /// </summary>
        private event EventHandler<BiometricMatchReplyEventArgs> BiometricMatchReplyReceived;

        /// <summary>
        /// Occurs when piv data reply is received.
        /// </summary>
        private event EventHandler<MultiPartMessageDataReplyEventArgs> PIVDataReplyReceived;

        /// <summary>
        /// Occurs when authentication challenge response is received.
        /// </summary>
        private event EventHandler<MultiPartMessageDataReplyEventArgs> AuthenticationChallengeResponseReceived;

        /// <summary>
        /// A negative reply that has been received.
        /// </summary>
        public class NakReplyEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="NakReplyEventArgs"/> class.
            /// </summary>
            /// <param name="connectionId">Identify the connection for communicating to the device.</param>
            /// <param name="address">Address assigned to the device.</param>
            /// <param name="nak">A negative reply that has been received.</param>
            public NakReplyEventArgs(Guid connectionId, byte address, Nak nak)
            {
                ConnectionId = connectionId;
                Address = address;
                Nak = nak;
            }

            /// <summary>
            /// Identify the connection for communicating to the device.
            /// </summary>
            public Guid ConnectionId { get; }

            /// <summary>
            /// Address assigned to the device.
            /// </summary>
            public byte Address { get; }

            /// <summary>
            /// A negative reply that has been received.
            /// </summary>
            public Nak Nak { get; }
        }

        /// <summary>
        /// A connection status has been changed.
        /// </summary>
        public class ConnectionStatusEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ConnectionStatusEventArgs"/> class.
            /// </summary>
            /// <param name="connectionId">Identify the connection for communicating to the device.</param>
            /// <param name="address">Address assigned to the device.</param>
            /// <param name="isConnected">Is the device currently connected.</param>
            /// <param name="isSecureChannelEstablished">Is the secure channel currently established.</param>
            public ConnectionStatusEventArgs(Guid connectionId, byte address, bool isConnected,
                bool isSecureChannelEstablished)
            {
                ConnectionId = connectionId;
                Address = address;
                IsConnected = isConnected;
                IsSecureChannelEstablished = isSecureChannelEstablished;
            }

            /// <summary>
            /// Identify the connection for communicating to the device.
            /// </summary>
            public Guid ConnectionId { get; }

            /// <summary>
            /// Address assigned to the device.
            /// </summary>
            public byte Address { get; }

            /// <summary>
            /// Is the device currently connected.
            /// </summary>
            public bool IsConnected { get; }

            /// <summary>
            /// Is the secure channel currently established
            /// </summary>
            public bool IsSecureChannelEstablished { get; }
        }

        /// <summary>
        /// The local status report reply has been received.
        /// </summary>
        public class LocalStatusReportReplyEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="LocalStatusReportReplyEventArgs"/> class.
            /// </summary>
            /// <param name="connectionId">Identify the connection for communicating to the device.</param>
            /// <param name="address">Address assigned to the device.</param>
            /// <param name="localStatus">A local status report reply.</param>
            public LocalStatusReportReplyEventArgs(Guid connectionId, byte address, LocalStatus localStatus)
            {
                ConnectionId = connectionId;
                Address = address;
                LocalStatus = localStatus;
            }

            /// <summary>
            /// Identify the connection for communicating to the device.
            /// </summary>
            public Guid ConnectionId { get; }

            /// <summary>
            /// Address assigned to the device.
            /// </summary>
            public byte Address { get; }

            /// <summary>
            /// A local status report reply.
            /// </summary>
            public LocalStatus LocalStatus { get; }
        }

        /// <summary>
        /// The input status report reply has been received.
        /// </summary>
        public class InputStatusReportReplyEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="LocalStatusReportReplyEventArgs" /> class.
            /// </summary>
            /// <param name="connectionId">Identify the connection for communicating to the device.</param>
            /// <param name="address">Address assigned to the device.</param>
            /// <param name="inputStatus">A input status report reply.</param>
            public InputStatusReportReplyEventArgs(Guid connectionId, byte address, InputStatus inputStatus)
            {
                ConnectionId = connectionId;
                Address = address;
                InputStatus = inputStatus;
            }

            /// <summary>
            /// Identify the connection for communicating to the device.
            /// </summary>
            public Guid ConnectionId { get; }

            /// <summary>
            /// Address assigned to the device.
            /// </summary>
            public byte Address { get; }

            /// <summary>
            /// A input status report reply.
            /// </summary>
            public InputStatus InputStatus { get; }
        }

        /// <summary>
        /// The output status report reply has been received.
        /// </summary>
        public class OutputStatusReportReplyEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OutputStatusReportReplyEventArgs"/> class.
            /// </summary>
            /// <param name="connectionId">Identify the connection for communicating to the device.</param>
            /// <param name="address">Address assigned to the device.</param>
            /// <param name="outputStatus">A output status report reply.</param>
            public OutputStatusReportReplyEventArgs(Guid connectionId, byte address, OutputStatus outputStatus)
            {
                ConnectionId = connectionId;
                Address = address;
                OutputStatus = outputStatus;
            }

            /// <summary>
            /// Identify the connection for communicating to the device.
            /// </summary>
            public Guid ConnectionId { get; }

            /// <summary>
            /// Address assigned to the device.
            /// </summary>
            public byte Address { get; }

            /// <summary>
            /// A output status report reply.
            /// </summary>
            public OutputStatus OutputStatus { get; }
        }

        /// <summary>
        /// The reader status report reply has been received.
        /// </summary>
        public class ReaderStatusReportReplyEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ReaderStatusReportReplyEventArgs"/> class.
            /// </summary>
            /// <param name="connectionId">Identify the connection for communicating to the device.</param>
            /// <param name="address">Address assigned to the device.</param>
            /// <param name="readerStatus">A reader status report reply.</param>
            public ReaderStatusReportReplyEventArgs(Guid connectionId, byte address, ReaderStatus readerStatus)
            {
                ConnectionId = connectionId;
                Address = address;
                ReaderStatus = readerStatus;
            }

            /// <summary>
            /// Identify the connection for communicating to the device.
            /// </summary>
            public Guid ConnectionId { get; }

            /// <summary>
            /// Address assigned to the device.
            /// </summary>
            public byte Address { get; }

            /// <summary>
            /// A reader status report reply.
            /// </summary>
            public ReaderStatus ReaderStatus { get; }
        }

        /// <summary>
        /// The raw card data reply has been received.
        /// </summary>
        public class RawCardDataReplyEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RawCardDataReplyEventArgs"/> class.
            /// </summary>
            /// <param name="connectionId">Identify the connection for communicating to the device.</param>
            /// <param name="address">Address assigned to the device.</param>
            /// <param name="rawCardData">A raw card data reply.</param>
            public RawCardDataReplyEventArgs(Guid connectionId, byte address, RawCardData rawCardData)
            {
                ConnectionId = connectionId;
                Address = address;
                RawCardData = rawCardData;
            }

            /// <summary>
            /// Identify the connection for communicating to the device.
            /// </summary>
            public Guid ConnectionId { get; }

            /// <summary>
            /// Address assigned to the device.
            /// </summary>
            public byte Address { get; }

            /// <summary>
            /// A raw card data reply.
            /// </summary>
            public RawCardData RawCardData { get; }
        }

        /// <summary>
        /// The manufacture specific reply has been received.
        /// </summary>
        public class ManufacturerSpecificReplyEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ManufacturerSpecificReplyEventArgs"/> class.
            /// </summary>
            /// <param name="connectionId">Identify the connection for communicating to the device.</param>
            /// <param name="address">Address assigned to the device.</param>
            /// <param name="manufacturerSpecific">A manufacturer specific reply.</param>
            public ManufacturerSpecificReplyEventArgs(Guid connectionId, byte address, ManufacturerSpecific manufacturerSpecific)
            {
                ConnectionId = connectionId;
                Address = address;
                ManufacturerSpecific = manufacturerSpecific;
            }

            /// <summary>
            /// Identify the connection for communicating to the device.
            /// </summary>
            public Guid ConnectionId { get; }

            /// <summary>
            /// Address assigned to the device.
            /// </summary>
            public byte Address { get; }

            /// <summary>
            /// A manufacturer specific reply.
            /// </summary>
            public ManufacturerSpecific ManufacturerSpecific { get; }
        }

        /// <summary>
        /// The extended read reply has been received.
        /// </summary>
        public class ExtendedReadReplyEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ExtendedReadReplyEventArgs"/> class.
            /// </summary>
            /// <param name="connectionId">Identify the connection for communicating to the device.</param>
            /// <param name="address">Address assigned to the device.</param>
            /// <param name="extendedRead">A extended read reply.</param>
            public ExtendedReadReplyEventArgs(Guid connectionId, byte address, ExtendedRead extendedRead)
            {
                ConnectionId = connectionId;
                Address = address;
                ExtendedRead = extendedRead;
            }

            /// <summary>
            /// Identify the connection for communicating to the device.
            /// </summary>
            public Guid ConnectionId { get; }

            /// <summary>
            /// Address assigned to the device.
            /// </summary>
            public byte Address { get; }

            /// <summary>
            /// A extended read reply.
            /// </summary>
            public ExtendedRead ExtendedRead { get; }
        }

        /// <summary>
        /// The multi-part message reply has been received.
        /// </summary>
        private class MultiPartMessageDataReplyEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MultiPartMessageDataReplyEventArgs"/> class.
            /// </summary>
            /// <param name="connectionId">Identify the connection for communicating to the device.</param>
            /// <param name="address">Address assigned to the device.</param>
            /// <param name="dataFragmentResponse">A PIV data reply.</param>
            public MultiPartMessageDataReplyEventArgs(Guid connectionId, byte address, DataFragmentResponse dataFragmentResponse)
            {
                ConnectionId = connectionId;
                Address = address;
                DataFragmentResponse = dataFragmentResponse;
            }

            /// <summary>
            /// Identify the connection for communicating to the device.
            /// </summary>
            public Guid ConnectionId { get; }

            /// <summary>
            /// Address assigned to the device.
            /// </summary>
            public byte Address { get; }

            /// <summary>
            /// A PIV data reply.
            /// </summary>
            public DataFragmentResponse DataFragmentResponse { get; }
        }

        /// <summary>
        /// The keypad reply has been received.
        /// </summary>
        public class KeypadReplyEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="KeypadReplyEventArgs"/> class.
            /// </summary>
            /// <param name="connectionId">Identify the connection for communicating to the device.</param>
            /// <param name="address">Address assigned to the device.</param>
            /// <param name="keypadData">A keypad reply.</param>
            public KeypadReplyEventArgs(Guid connectionId, byte address, KeypadData keypadData)
            {
                ConnectionId = connectionId;
                Address = address;
                KeypadData = keypadData;
            }

            /// <summary>
            /// Identify the connection for communicating to the device.
            /// </summary>
            public Guid ConnectionId { get; }

            /// <summary>
            /// Address assigned to the device.
            /// </summary>
            public byte Address { get; }

            /// <summary>
            /// A keypad reply..
            /// </summary>
            public KeypadData KeypadData { get; }
        }

        /// <summary>
        /// A biometric match reply has been received.
        /// </summary>
        private class BiometricReadResultsReplyEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="BiometricReadResultsReplyEventArgs"/> class.
            /// </summary>
            /// <param name="connectionId">Identify the connection for communicating to the device.</param>
            /// <param name="address">Address assigned to the device.</param>
            /// <param name="biometricReadResult">A biometric read results reply.</param>
            public BiometricReadResultsReplyEventArgs(Guid connectionId, byte address,
                BiometricReadResult biometricReadResult)
            {
                ConnectionId = connectionId;
                Address = address;
                BiometricReadResult = biometricReadResult;
            }

            /// <summary>
            /// Identify the connection for communicating to the device.
            /// </summary>
            public Guid ConnectionId { get; }

            /// <summary>
            /// Address assigned to the device.
            /// </summary>
            public byte Address { get; }

            /// <summary>
            /// A biometric read result reply.
            /// </summary>
            public BiometricReadResult BiometricReadResult { get; }
        }

        /// <summary>
        /// A biometric match reply has been received.
        /// </summary>
        private class BiometricMatchReplyEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="BiometricMatchReplyEventArgs"/> class.
            /// </summary>
            /// <param name="connectionId">Identify the connection for communicating to the device.</param>
            /// <param name="address">Address assigned to the device.</param>
            /// <param name="biometricMatchResult">A biometric match reply.</param>
            public BiometricMatchReplyEventArgs(Guid connectionId, byte address,
                BiometricMatchResult biometricMatchResult)
            {
                ConnectionId = connectionId;
                Address = address;
                BiometricMatchResult = biometricMatchResult;
            }

            /// <summary>
            /// Identify the connection for communicating to the device.
            /// </summary>
            public Guid ConnectionId { get; }

            /// <summary>
            /// Address assigned to the device.
            /// </summary>
            public byte Address { get; }

            /// <summary>
            /// A biometric match result reply..
            /// </summary>
            public BiometricMatchResult BiometricMatchResult { get; }
        }

        /// <summary>
        /// Track the status of a file transfer to a PD
        /// </summary>
        public class FileTransferStatus
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="FileTransferStatus"/> class.
            /// </summary>
            /// <param name="status">The last status returned from the PD</param>
            /// <param name="currentOffset">The current offset in the data of the file transfer</param>
            /// <param name="nak">Contains Nak reply data if returned</param>
            public FileTransferStatus(Model.ReplyData.FileTransferStatus.StatusDetail status, int currentOffset,
                Nak nak)
            {
                Status = status;
                CurrentOffset = currentOffset;
                Nak = nak;
            }

            /// <summary>
            /// The last status returned from the PD
            /// </summary>
            public Model.ReplyData.FileTransferStatus.StatusDetail  Status { get; }

            /// <summary>
            /// The current offset in the data of the file transfer
            /// </summary>
            public int CurrentOffset { get; }

            /// <summary>
            /// Contains Nak reply if returned
            /// </summary>
            public Nak Nak { get; }
        }

        private class ReplyEventArgs : EventArgs
        {
            public Reply Reply { get; set; }
        }
    }
}