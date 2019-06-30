using System;
using OSDP.Net;
using OSDP.Net.Messages;
using Terminal.Gui;

namespace Console
{
    internal static class Program
    {
        static void Main()
        {
            var controlPanel = new ControlPanel();

            Guid id = controlPanel.StartConnection(new SerialPortOsdpConnection());
            
            Application.Init ();
            var top = Application.Top;

            var window = new Window ("OSDP.Net") {
                X = 0,
                Y = 1, // Leave one row for the toplevel menu

                Width = Dim.Fill (),
                Height = Dim.Fill ()
            };
            top.Add (window);
            
            var menu = new MenuBar (new MenuBarItem [] {
                new MenuBarItem ("_File", new MenuItem [] {
                    new MenuItem ("_Quit", "", () => { top.Running = false; })
                })
            });
            top.Add (menu);
            
            Application.Run();

            /*
            System.Console.WriteLine("1 - Send Id Report");
            System.Console.WriteLine("0 - Exit");

            int command;
            do
            {
                int.TryParse(System.Console.ReadLine(), out command);

                if (command == 1)
                {
                    
                    controlPanel.SendCommand(id, new IdReportCommand(0));
                }
            } while (command != 0);
*/


            controlPanel.Shutdown();
        }
    }
}

