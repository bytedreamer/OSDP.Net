using System;
using OSDP.Net;

namespace Console
{
    internal static class Program
    {
        static void Main()
        {
            var controlPanel = new ControlPanel();

            Guid id = controlPanel.StartConnection(new SerialPortOsdpConnection());


            System.Console.WriteLine("Press enter to shutdown connection");
            System.Console.ReadKey();

            controlPanel.Shutdown();
        }
    }
}

