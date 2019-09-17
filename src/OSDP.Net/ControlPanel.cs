using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using OSDP.Net.Connections;
using OSDP.Net.Logging;
using OSDP.Net.Messages;
using OSDP.Net.Model.ReplyData;

namespace OSDP.Net
{
    /// <summary>
    /// The OSDP control panel
    /// </summary>
    public class ControlPanel
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly ConcurrentBag<Bus> _buses = new ConcurrentBag<Bus>();
        private readonly BlockingCollection<Reply> _replies = new BlockingCollection<Reply>();
        private readonly TimeSpan _replyResponseTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Default constructor
        /// </summary>
        public ControlPanel()
        {
            Task.Factory.StartNew(() =>
            {
                foreach (var reply in _replies.GetConsumingEnumerable())
                {
                    Logger.Debug($"Received a reply {reply}");
                    
                    OnReplyReceived(reply);
                    
                    if (reply.Type == ReplyType.FormattedReaderData)
                    {
                        Logger.Debug(
                            $"Formatted Reader Data {BitConverter.ToString(reply.ExtractReplyData.ToArray())}");
                    }
                    else if (reply.Type == ReplyType.RawReaderData)
                    {
                        Logger.Debug(
                            $"Raw Reader Data: {BitConverter.ToString(reply.ExtractReplyData.ToArray())}");
                    }
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
            var newBus = new Bus(connection, _replies);
            
            _buses.Add(newBus);

            Task.Factory.StartNew(async () =>
            {
                await newBus.StartPollingAsync();
            }, TaskCreationOptions.LongRunning);

            return newBus.Id;
        }

        public async Task<DeviceIdentification> IdReport(Guid connectionId, byte address)
        {
            return DeviceIdentification.CreateDeviceIdentification(await SendCommand(connectionId,
                new IdReportCommand(address)));
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

            if (source.Task == await Task.WhenAny(source.Task, Task.Delay(_replyResponseTimeout)))
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
                bus.Close();
            }
        }

        /// <summary>
        /// Add a PD to the control panel
        /// </summary>
        /// <param name="connectionId">Identify the connection for communicating to the device</param>
        /// <param name="address">Address assigned to the device</param>
        /// <param name="useSecureChannel">Require the device to communicate with a secure channel</param>
        public void AddDevice(Guid connectionId, byte address, bool useSecureChannel)
        {
            _buses.FirstOrDefault(bus => bus.Id == connectionId)?.AddDevice(address, useSecureChannel);
        }

        public void RemoveDevice(Guid connectionId, byte address)
        {
            _buses.FirstOrDefault(bus => bus.Id == connectionId)?.RemoveDevice(address);
        }

        internal virtual void OnReplyReceived(Reply reply)
        {
            var handler = ReplyReceived;
            handler?.Invoke(this, new ReplyEventArgs {Reply = reply});
        }

        private event EventHandler<ReplyEventArgs> ReplyReceived;

        private class ReplyEventArgs : EventArgs
        {
            public Reply Reply { get; set; }
        }
    }
}