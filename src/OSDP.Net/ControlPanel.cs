using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using OSDP.Net.Messages;

namespace OSDP.Net
{
    public class ControlPanel
    {
        private readonly ConcurrentDictionary<Guid, Bus> _buses = new ConcurrentDictionary<Guid, Bus>();
        private readonly BlockingCollection<Reply> _replies = new BlockingCollection<Reply>();

        public ControlPanel()
        {
            Task.Factory.StartNew(() =>
            {
                foreach (var reply in _replies.GetConsumingEnumerable())
                {
                    Console.WriteLine($"Received a reply {reply}");
                }
            }, TaskCreationOptions.LongRunning);
        }

        public Guid StartConnection(IOsdpConnection connection)
        {
            var newBus = new Bus(connection, _replies);
            Guid id = Guid.NewGuid();
            
            _buses[id] = newBus;

            Task.Factory.StartNew(async () =>
            {
                await newBus.StartPollingAsync();
            }, TaskCreationOptions.LongRunning);

            return id;
        }

        public void Shutdown()
        {
            foreach (var bus in _buses)
            {
                bus.Value.Close();
            }
        }
    }
}