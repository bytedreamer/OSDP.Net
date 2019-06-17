using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using OSDP.Net.Messages;

namespace OSDP.Net
{
    public class ControlPanel
    {
        private readonly ConcurrentBag<Bus> _buses = new ConcurrentBag<Bus>();
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
            
            _buses.Add(newBus);

            Task.Factory.StartNew(async () =>
            {
                await newBus.StartPollingAsync();
            }, TaskCreationOptions.LongRunning);

            return newBus.Id;
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