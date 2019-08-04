using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using OSDP.Net.Connections;
using OSDP.Net.Logging;
using OSDP.Net.Messages;

namespace OSDP.Net
{
    public class ControlPanel
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly ConcurrentBag<Bus> _buses = new ConcurrentBag<Bus>();
        private readonly BlockingCollection<Reply> _replies = new BlockingCollection<Reply>();
        private readonly TimeSpan ReplyResponseTimeout = TimeSpan.FromSeconds(5);

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

        public async Task<Reply> SendCommand(Guid connectionId, Command command)
        {
            var tcs = new TaskCompletionSource<Reply>();

            void EventHandler(object sender, ReplyEventArgs replyEventArgs)
            {
                if (!replyEventArgs.Reply.MatchIssuingCommand(command)) return;
                ReplyReceived -= EventHandler;
                tcs.SetResult(replyEventArgs.Reply);
            }

            ReplyReceived += EventHandler;
            
            _buses.FirstOrDefault(bus => bus.Id == connectionId)?.SendCommand(command);

            if (tcs.Task == await Task.WhenAny(tcs.Task, Task.Delay(ReplyResponseTimeout)))
            {
                return await tcs.Task;
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

        public void AddDevice(Guid connectionId, byte address)
        {
            _buses.FirstOrDefault(bus => bus.Id == connectionId)?.AddDevice(address);
        }

        public void RemoveDevice(Guid connectionId, byte address)
        {
            _buses.FirstOrDefault(bus => bus.Id == connectionId)?.RemoveDevice(address);
        }

        private event EventHandler<ReplyEventArgs> ReplyReceived;

        protected virtual void OnReplyReceived(Reply reply)
        {
            var handler = ReplyReceived;
            handler?.Invoke(this, new ReplyEventArgs {Reply = reply});
        }

        private class ReplyEventArgs : EventArgs
        {
            public Reply Reply { get; set; }
        }
    }
}