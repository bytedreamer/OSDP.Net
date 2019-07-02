using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;
using OSDP.Net;
using OSDP.Net.Messages;
using Terminal.Gui;

namespace Console
{
    internal static class Program
    {
        private static readonly ControlPanel ControlPanel = new ControlPanel();
        private static readonly TextView MessageView = new TextView();
        private static readonly Queue<string> Messages = new Queue<string>();
        private static readonly object MessageLock =  new object();

        static void Main()
        {
            XmlConfigurator.Configure(
                LogManager.GetRepository(Assembly.GetAssembly(typeof(LogManager))),
                new FileInfo("log4net.config"));

            Guid id = ControlPanel.StartConnection(new SerialPortOsdpConnection());

            Application.Init();
            var top = Application.Top;

            var window = new Window("OSDP.Net Logging")
            {
                X = 0,
                Y = 1, // Leave one row for the toplevel menu

                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            top.Add(window);

            var menu = new MenuBar(new[]
            {
                new MenuBarItem("_File", new[]
                {
                    new MenuItem("_Quit", "", () => { top.Running = false; })
                }),
                new MenuBarItem("_Command", new[]
                {
                    new MenuItem("_ID Report", "", () => { ControlPanel.SendCommand(id, new IdReportCommand(0)); })
                })
            });
            top.Add(menu);

            window.Add(MessageView);

            Application.Run();

            ControlPanel.Shutdown();
        }

        public static void AddLogMessage(string message)
        {
            lock (MessageLock)
            {
                Messages.Enqueue(message);
                while (Messages.Count > MessageView.Bounds.Height)
                {
                    Messages.Dequeue();
                }

                MessageView.Text = string.Join("", Messages.ToArray());
            }
        }
    }
}

