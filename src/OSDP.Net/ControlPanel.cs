using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OSDP.Net
{
    public class ControlPanel
    {
        private readonly List<Bus> _buses = new List<Bus>();

        public async Task AddBus(Bus bus, CancellationToken token)
        {
            _buses.Add(bus);
            await bus.StartPollingAsync(0, token);
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