using System;
using OSDP.Net;
using OSDP.Net.Messages;

namespace Console
{
    internal static class Program
    {
        static void Main()
        {
            var controlPanel = new ControlPanel();

            Guid id = controlPanel.StartConnection(new SerialPortOsdpConnection());

            System.Console.WriteLine("1 - Send Id Report");
            System.Console.WriteLine("0 - Exit");

            int command;
            do
            {
                command = Convert.ToInt32(System.Console.ReadLine());

                if (command == 1)
                {
                    
                    controlPanel.SendCommand(id, new IdReportCommand(0));
                }
            } while (command != 0);


            controlPanel.Shutdown();
        }
    }
}

