using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OSDP.Net.Connections;
using OSDP.Net.Messages;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Model.ReplyData;
using CommunicationConfiguration = OSDP.Net.Model.CommandData.CommunicationConfiguration;
using ManufacturerSpecific = OSDP.Net.Model.ReplyData.ManufacturerSpecific;

namespace OSDP.Net
{
    /// <summary>The OSDP control panel used to communicate to Peripheral Devices (PDs) as an Access Control Unit (ACU). If multiple connections are needed, add them to the control panel. Avoid creating multiple control panel objects.</summary>
    public class ControlPanel
    {
        private readonly ConcurrentBag<Bus> _buses = new ConcurrentBag<Bus>();
        private readonly ILogger<ControlPanel> _logger;
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _pivDataLocks = new ConcurrentDictionary<int, SemaphoreSlim>();
        private readonly BlockingCollection<Reply> _replies = new BlockingCollection<Reply>();
        private readonly TimeSpan _replyResponseTimeout = TimeSpan.FromSeconds(5);


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
        public Guid StartConnection(IOsdpConnection connection)
        {
            return StartConnection(connection, Bus.DefaultPollInterval);
        }

        /// <summary>
        /// Start polling on the defined connection.
        /// </summary>
        /// <param name="connection">This represents the type of connection used for communicating to PDs.</param>
        /// <param name="pollInterval">The interval at which the devices will be polled</param>
        /// <returns>An identifier that represents the connection</returns>
        public Guid StartConnection(IOsdpConnection connection, TimeSpan pollInterval)
        {
            var newBus = new Bus(connection, _replies, pollInterval, _logger);
            
            newBus.ConnectionStatusChanged += BusOnConnectionStatusChanged;
            
            _buses.Add(newBus);

            Task.Factory.StartNew(async () =>
            {
                await newBus.StartPollingAsync().ConfigureAwait(false);
            }, TaskCreationOptions.LongRunning);

            return newBus.Id;
        }

        private void BusOnConnectionStatusChanged(object sender, Bus.ConnectionStatusEventArgs eventArgs)
        {
            if (sender is Bus bus) OnConnectionStatusChanged(bus.Id, eventArgs.Address, eventArgs.IsConnected);
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
        /// Request to get PIV data from PD.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        /// <param name="getPIVData">Describe the PIV data to retrieve.</param>
        /// <param name="timeout">A TimeSpan that represents the number of milliseconds to wait, a TimeSpan that represents -1 milliseconds to wait indefinitely, or a TimeSpan that represents 0 milliseconds to test the wait handle and return immediately.</param>
        /// <param name="cancellationToken">The CancellationToken token to observe.</param>
        /// <returns>A response with the PIV data requested.</returns>
        public async Task<byte[]> GetPIVData(Guid connectionId, byte address, GetPIVData getPIVData, TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var pivDataLock = GetPIVDataLock(connectionId, address);
            
            if (!await pivDataLock.WaitAsync(timeout, cancellationToken))
            {
                throw new TimeoutException("Timeout waiting for another PIV data request to complete.");
            }
            
            try
            {
                return await WaitForPIVData(connectionId, address, getPIVData, timeout, cancellationToken);
            }
            finally
            {
                pivDataLock.Release();
            }
        }

        private SemaphoreSlim GetPIVDataLock(Guid connectionId, byte address)
        {
            int hash = new { connectionId, address }.GetHashCode();
            
            if (_pivDataLocks.TryGetValue(hash, out var pivDataLock))
            {
                return pivDataLock;
            }

            var newPIVDataLock = new SemaphoreSlim(1, 1);
            _pivDataLocks[hash] = newPIVDataLock;
            return newPIVDataLock;
        }

        private async Task<byte[]> WaitForPIVData(Guid connectionId, byte address, GetPIVData getPIVData, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            bool complete = false;
            DateTime endTime = DateTime.UtcNow + timeout;
            byte[] data = null;

            void Handler(object sender, PIVDataReplyEventArgs args)
            {
                // Only process matching replies
                if (args.ConnectionId != connectionId || args.Address != address) return;

                var pivData = args.PIVData;
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
                    await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
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
        /// Is the PD online
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device</param>
        /// <param name="address">Address assigned to the device</param>
        /// <returns>Returns true if the PD is online</returns>
        public bool IsOnline(Guid connectionId, byte address)
        {
            return _buses.First(bus => bus.Id == connectionId).IsOnline(address);
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

            _buses.FirstOrDefault(bus => bus.Id == connectionId)?.SendCommand(command);

            if (source.Task == await Task.WhenAny(source.Task, Task.Delay(_replyResponseTimeout, cancellationToken))
                .ConfigureAwait(false))
            {
                return await source.Task;
            }
            else
            {
                ReplyReceived -= EventHandler;
                throw new TimeoutException();
            }
        }

        /// <summary>
        /// Shutdown the control panel and stop all communication to PDs.
        /// </summary>
        public void Shutdown()
        {
            foreach (var bus in _buses)
            {
                bus.ConnectionStatusChanged -= BusOnConnectionStatusChanged;
                
                bus.Close();
                
                foreach (byte address in bus.ConfigureDeviceAddresses)
                {
                    OnConnectionStatusChanged(bus.Id, address, false);
                }
            }
            while (!_buses.IsEmpty) 
            {
                _buses.TryTake(out _);
            }

            foreach (var pivDataLock in _pivDataLocks.Values)
            {
                pivDataLock.Dispose();
            }
            _pivDataLocks.Clear();
        }

        /// <summary>
        /// Reset communication sequence with the PD specified.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device.</param>
        /// <param name="address">Address assigned to the device.</param>
        public void ResetDevice(Guid connectionId, int address)
        {
            _buses.FirstOrDefault(bus => bus.Id == connectionId)?.ResetDevice(address);
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
            var foundBus = _buses.FirstOrDefault(bus => bus.Id == connectionId);
            if (foundBus == null)
            {
                throw new ArgumentException( "Connection could not be found", nameof(connectionId));
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
            _buses.FirstOrDefault(bus => bus.Id == connectionId)?.RemoveDevice(address);
        }

        private void OnConnectionStatusChanged(Guid connectionId, byte address, bool isConnected)
        {
            var handler = ConnectionStatusChanged;
            handler?.Invoke(this, new ConnectionStatusEventArgs(connectionId, address, isConnected));
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
                        new PIVDataReplyEventArgs(reply.ConnectionId, reply.Address,
                            PIVData.ParseData(reply.ExtractReplyData)));   
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
            }
        }

        private event EventHandler<ReplyEventArgs> ReplyReceived;

        /// <summary>
        /// Occurs when connection status changed.
        /// </summary>
        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Occurs when NAK reply received.
        /// </summary>
        public event EventHandler<NakReplyEventArgs> NakReplyReceived;

        /// <summary>
        /// Occurs when local status report reply received.
        /// </summary>
        public event EventHandler<LocalStatusReportReplyEventArgs> LocalStatusReportReplyReceived;

        /// <summary>
        /// Occurs when input status report reply received.
        /// </summary>
        public event EventHandler<InputStatusReportReplyEventArgs> InputStatusReportReplyReceived;

        /// <summary>
        /// Occurs when output status report reply received.
        /// </summary>
        public event EventHandler<OutputStatusReportReplyEventArgs> OutputStatusReportReplyReceived;

        /// <summary>
        /// Occurs when reader status report reply received.
        /// </summary>
        public event EventHandler<ReaderStatusReportReplyEventArgs> ReaderStatusReportReplyReceived;

        /// <summary>
        /// Occurs when raw card data reply received.
        /// </summary>
        public event EventHandler<RawCardDataReplyEventArgs> RawCardDataReplyReceived;

        /// <summary>
        /// Occurs when manufacturer specific reply received.
        /// </summary>
        public event EventHandler<ManufacturerSpecificReplyEventArgs> ManufacturerSpecificReplyReceived;

        /// <summary>
        /// Occurs when extended read reply received.
        /// </summary>
        public event EventHandler<ExtendedReadReplyEventArgs> ExtendedReadReplyReceived;

        /// <summary>
        /// Occurs when key pad data reply received.
        /// </summary>
        public event EventHandler<KeypadReplyEventArgs> KeypadReplyReceived;

        /// <summary>
        /// Occurs when piv data reply received.
        /// </summary>
        private event EventHandler<PIVDataReplyEventArgs> PIVDataReplyReceived;

        public class NakReplyEventArgs : EventArgs
        {
            public NakReplyEventArgs(Guid connectionId, byte address, Nak nak)
            {
                ConnectionId = connectionId;
                Address = address;
                Nak = nak;
            }

            public Guid ConnectionId { get; }
            public byte Address { get; }
            public Nak Nak { get; }
        }

        public class ConnectionStatusEventArgs : EventArgs
        {
            public ConnectionStatusEventArgs(Guid connectionId, byte address, bool isConnected)
            {
                ConnectionId = connectionId;
                Address = address;
                IsConnected = isConnected;
            }

            public Guid ConnectionId { get; }
            public byte Address { get; }
            public bool IsConnected { get; }
        }

        public class LocalStatusReportReplyEventArgs : EventArgs
        {
            public LocalStatusReportReplyEventArgs(Guid connectionId, byte address, LocalStatus localStatus)
            {
                ConnectionId = connectionId;
                Address = address;
                LocalStatus = localStatus;
            }

            public Guid ConnectionId { get; }
            public byte Address { get; }
            public LocalStatus LocalStatus { get; }
        }

        public class InputStatusReportReplyEventArgs : EventArgs
        {
            public InputStatusReportReplyEventArgs(Guid connectionId, byte address, InputStatus inputStatus)
            {
                ConnectionId = connectionId;
                Address = address;
                InputStatus = inputStatus;
            }

            public Guid ConnectionId { get; }
            public byte Address { get; }
            public InputStatus InputStatus { get; }
        }

        public class OutputStatusReportReplyEventArgs : EventArgs
        {
            public OutputStatusReportReplyEventArgs(Guid connectionId, byte address, OutputStatus outputStatus)
            {
                ConnectionId = connectionId;
                Address = address;
                OutputStatus = outputStatus;
            }

            public Guid ConnectionId { get; }
            public byte Address { get; }
            public OutputStatus OutputStatus { get; }
        }

        public class ReaderStatusReportReplyEventArgs : EventArgs
        {
            public ReaderStatusReportReplyEventArgs(Guid connectionId, byte address, ReaderStatus readerStatus)
            {
                ConnectionId = connectionId;
                Address = address;
                ReaderStatus = readerStatus;
            }

            public Guid ConnectionId { get; }
            public byte Address { get; }
            public ReaderStatus ReaderStatus { get; }
        }

        public class RawCardDataReplyEventArgs : EventArgs
        {
            public RawCardDataReplyEventArgs(Guid connectionId, byte address, RawCardData rawCardData)
            {
                ConnectionId = connectionId;
                Address = address;
                RawCardData = rawCardData;
            }

            public Guid ConnectionId { get; }
            public byte Address { get; }
            public RawCardData RawCardData { get; }
        }

        public class ManufacturerSpecificReplyEventArgs : EventArgs
        {
            public ManufacturerSpecificReplyEventArgs(Guid connectionId, byte address, ManufacturerSpecific manufacturerSpecific)
            {
                ConnectionId = connectionId;
                Address = address;
                ManufacturerSpecific = manufacturerSpecific;
            }

            public Guid ConnectionId { get; }

            public byte Address { get; }

            public ManufacturerSpecific ManufacturerSpecific { get; }
        }

        public class ExtendedReadReplyEventArgs : EventArgs
        {
            public ExtendedReadReplyEventArgs(Guid connectionId, byte address, ExtendedRead extendedRead)
            {
                ConnectionId = connectionId;
                Address = address;
                ExtendedRead = extendedRead;
            }

            public Guid ConnectionId { get; }

            public byte Address { get; }

            public ExtendedRead ExtendedRead { get; }
        }

        private class PIVDataReplyEventArgs : EventArgs
        {
            public PIVDataReplyEventArgs(Guid connectionId, byte address, PIVData pivData)
            {
                ConnectionId = connectionId;
                Address = address;
                PIVData = pivData;
            }

            public Guid ConnectionId { get; }

            public byte Address { get; }

            public PIVData PIVData { get; }
        }

        public class KeypadReplyEventArgs : EventArgs
        {
            public KeypadReplyEventArgs(Guid connectionId, byte address, KeypadData keypadData)
            {
                ConnectionId = connectionId;
                Address = address;
                KeypadData = keypadData;
            }

            public Guid ConnectionId { get; }

            public byte Address { get; }

            public KeypadData KeypadData { get; }
        }

        private class ReplyEventArgs : EventArgs
        {
            public Reply Reply { get; set; }
        }
    }
}