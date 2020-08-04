using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OSDP.Net.Connections;
using OSDP.Net.Messages;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Model.ReplyData;
using CommunicationConfiguration = OSDP.Net.Model.CommandData.CommunicationConfiguration;

namespace OSDP.Net
{
    /// <summary>
    /// The OSDP control panel
    /// </summary>
    public class ControlPanel
    {
        private readonly ConcurrentBag<Bus> _buses = new ConcurrentBag<Bus>();
        private readonly ILogger<ControlPanel> _logger;
        private readonly BlockingCollection<Reply> _replies = new BlockingCollection<Reply>();
        private readonly TimeSpan _replyResponseTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Default constructor
        /// </summary>
        public ControlPanel(ILogger<ControlPanel> logger = null)
        {
            _logger = logger;
            
            Task.Factory.StartNew(() =>
            {
                foreach (var reply in _replies.GetConsumingEnumerable())
                {
                    _logger?.LogDebug($"Received a reply {reply}");
                    
                    OnReplyReceived(reply);
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Start polling on a connection
        /// </summary>
        /// <param name="connection">Details of the connection</param>
        /// <returns>The id of the connection</returns>
        public Guid StartConnection(IOsdpConnection connection)
        {
            var newBus = new Bus(connection, _replies, _logger);
            
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
        /// 
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="command"></param>
        public async Task SendCustomCommand(Guid connectionId, Command command)
        {
            await SendCommand(connectionId, command).ConfigureAwait(false);
        }

        public async Task<DeviceIdentification> IdReport(Guid connectionId, byte address)
        {
            return DeviceIdentification.ParseData(await SendCommand(connectionId,
                new IdReportCommand(address)).ConfigureAwait(false));
        }

        public async Task<DeviceCapabilities> DeviceCapabilities(Guid connectionId, byte address)
        {
            return Model.ReplyData.DeviceCapabilities.ParseData(await SendCommand(connectionId,
                new DeviceCapabilitiesCommand(address)).ConfigureAwait(false));
        }

        public async Task<LocalStatus> LocalStatus(Guid connectionId, byte address)
        {
            return Model.ReplyData.LocalStatus.ParseData(await SendCommand(connectionId,
                new LocalStatusReportCommand(address)).ConfigureAwait(false));
        }

        public async Task<InputStatus> InputStatus(Guid connectionId, byte address)
        {
            return Model.ReplyData.InputStatus.ParseData(await SendCommand(connectionId,
                new InputStatusReportCommand(address)).ConfigureAwait(false));
        }

        public async Task<OutputStatus> OutputStatus(Guid connectionId, byte address)
        {
            return Model.ReplyData.OutputStatus.ParseData(await SendCommand(connectionId,
                new OutputStatusReportCommand(address)).ConfigureAwait(false));
        }

        public async Task<ReaderStatus> ReaderStatus(Guid connectionId, byte address)
        {
            return Model.ReplyData.ReaderStatus.ParseData(await SendCommand(connectionId,
                new ReaderStatusReportCommand(address)).ConfigureAwait(false));
        }

        public async Task<bool> OutputControl(Guid connectionId, byte address, OutputControls outputControls)
        {
            var reply = await SendCommand(connectionId,
                new OutputControlCommand(address, outputControls)).ConfigureAwait(false);
            
            return reply.Type == ReplyType.Ack || reply.Type == ReplyType.OutputStatusReport;
        }

        public async Task<bool> ReaderLedControl(Guid connectionId, byte address, ReaderLedControls readerLedControls)
        {
            var reply = await SendCommand(connectionId,
                new ReaderLedControlCommand(address, readerLedControls)).ConfigureAwait(false);
            
            return reply.Type == ReplyType.Ack;
        }

        public async Task<bool> ReaderBuzzerControl(Guid connectionId, byte address, ReaderBuzzerControl readerBuzzerControl)
        {
            var reply = await SendCommand(connectionId,
                new ReaderBuzzerControlCommand(address, readerBuzzerControl)).ConfigureAwait(false);
            
            return reply.Type == ReplyType.Ack;
        }

        public async Task<bool> ReaderTextOutput(Guid connectionId, byte address, ReaderTextOutput readerTextOutput)
        {
            var reply = await SendCommand(connectionId,
                new ReaderTextOutputCommand(address, readerTextOutput)).ConfigureAwait(false);
            
            return reply.Type == ReplyType.Ack;
        }

        public async Task<Model.ReplyData.CommunicationConfiguration> CommunicationConfiguration(Guid connectionId,
            byte address, CommunicationConfiguration communicationConfiguration)
        {
            return Model.ReplyData.CommunicationConfiguration.ParseData(await SendCommand(connectionId,
                new CommunicationSetCommand(address, communicationConfiguration)).ConfigureAwait(false));
        }

        public bool IsOnline(Guid connectionId, byte address)
        {
            return _buses.First(bus => bus.Id == connectionId).IsOnline(address);
        }

        internal async Task<Reply> SendCommand(Guid connectionId, Command command)
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

            if (source.Task == await Task.WhenAny(source.Task, Task.Delay(_replyResponseTimeout)).ConfigureAwait(false))
            {
                return await source.Task;
            }
            else
            {
                ReplyReceived -= EventHandler;
                throw new TimeoutException();
            }
        }

        public void Shutdown()
        {
            foreach (var bus in _buses)
            {
                bus.ConnectionStatusChanged -= BusOnConnectionStatusChanged;
                
                bus.Close();
            }
        }

        /// <summary>
        /// Add a PD to the control panel. It will replace existing PD that is configured at the same address.
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device</param>
        /// <param name="address">Address assigned to the device</param>
        /// <param name="useCrc">Use CRC for error checking</param>
        /// <param name="useSecureChannel">Require the device to communicate with a secure channel</param>
        /// <param name="secureChannelKey">Set the secure channel key, default is used if not specified</param>
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
        /// <param name="connectionId"></param>
        /// <param name="address"></param>
        public void RemoveDevice(Guid connectionId, byte address)
        {
            _buses.FirstOrDefault(bus => bus.Id == connectionId)?.RemoveDevice(address);
        }

        internal void OnConnectionStatusChanged(Guid connectionId, byte address, bool isConnected)
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
                        new NakReplyEventArgs(reply.ConnectionId, reply.Address, Nak.ParseData(reply)));
                    break;
                }
                case ReplyType.LocalStatusReport:
                {
                    var handler = LocalStatusReportReplyReceived;
                    handler?.Invoke(this,
                        new LocalStatusReportReplyEventArgs(reply.ConnectionId, reply.Address,
                            Model.ReplyData.LocalStatus.ParseData(reply)));
                    break;
                }
                case ReplyType.InputStatusReport:
                {
                    var handler = InputStatusReportReplyReceived;
                    handler?.Invoke(this,
                        new InputStatusReportReplyEventArgs(reply.ConnectionId, reply.Address,
                            Model.ReplyData.InputStatus.ParseData(reply)));
                    break;
                }
                case ReplyType.OutputStatusReport:
                {
                    var handler = OutputStatusReportReplyReceived;
                    handler?.Invoke(this,
                        new OutputStatusReportReplyEventArgs(reply.ConnectionId, reply.Address,
                            Model.ReplyData.OutputStatus.ParseData(reply)));
                    break;
                }
                case ReplyType.ReaderStatusReport:
                {
                    var handler = ReaderStatusReportReplyReceived;
                    handler?.Invoke(this,
                        new ReaderStatusReportReplyEventArgs(reply.ConnectionId, reply.Address,
                            Model.ReplyData.ReaderStatus.ParseData(reply)));
                    break;
                }

                case ReplyType.RawReaderData:
                {
                    var handler = RawCardDataReplyReceived;
                    handler?.Invoke(this,
                        new RawCardDataReplyEventArgs(reply.ConnectionId, reply.Address, RawCardData.ParseData(reply)));
                    break;
                }
            }
        }

        private event EventHandler<ReplyEventArgs> ReplyReceived;

        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;

        public event EventHandler<NakReplyEventArgs> NakReplyReceived;

        public event EventHandler<LocalStatusReportReplyEventArgs> LocalStatusReportReplyReceived;

        public event EventHandler<InputStatusReportReplyEventArgs> InputStatusReportReplyReceived;

        public event EventHandler<OutputStatusReportReplyEventArgs> OutputStatusReportReplyReceived;

        public event EventHandler<ReaderStatusReportReplyEventArgs> ReaderStatusReportReplyReceived;

        public event EventHandler<RawCardDataReplyEventArgs> RawCardDataReplyReceived;

        public class NakReplyEventArgs
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

        public class ConnectionStatusEventArgs
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

        public class LocalStatusReportReplyEventArgs
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

        public class InputStatusReportReplyEventArgs
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

        public class OutputStatusReportReplyEventArgs
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

        public class ReaderStatusReportReplyEventArgs
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

        public class RawCardDataReplyEventArgs
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

        private class ReplyEventArgs : EventArgs
        {
            public Reply Reply { get; set; }
        }
    }
}