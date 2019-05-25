using System;
using System.Threading.Tasks;
using OSDP.Net;
using OSDP.Net.Messages;

namespace Console
{
    class Program
    {
        static async Task Main(string[] args) 
        {
            var controlPanel = new ControlPanel(new SerialPortOsdpConnection());

            System.Console.WriteLine(BitConverter.ToString(await controlPanel.SendCommand(new PollCommand())));

            System.Console.ReadKey();

            controlPanel.Shutdown(); 
        }
    }
}

