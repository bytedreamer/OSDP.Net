using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using OSDP.Net.Logging;
using OSDP.Net.Messages;

namespace OSDP.Net
{
    public class ControlPanel
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly ConcurrentBag<Bus> _buses = new ConcurrentBag<Bus>();
        private readonly BlockingCollection<Reply> _replies = new BlockingCollection<Reply>();

        public ControlPanel()
        {
            Task.Factory.StartNew(() =>
            {
                foreach (var reply in _replies.GetConsumingEnumerable())
                {
                    Logger.Debug($"Received a reply {reply}");
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

        public void SendCommand(Guid connectionId, Command command)
        {
            _buses.FirstOrDefault(bus => bus.Id == connectionId)?.SendCommand(command);
        }

        public void Shutdown()
        {
            foreach (var bus in _buses)
            {
                bus.Close();
            }
        }
    }
}