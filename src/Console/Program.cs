using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using Console.Configuration;
using log4net;
using log4net.Config;
using OSDP.Net;
using OSDP.Net.Connections;
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

            var settings = GetConnectionSettings();

            Guid id = ControlPanel.StartConnection(new SerialPortOsdpConnection(
                settings.ConnectionSettings.PortName,
                settings.ConnectionSettings.BaudRate));

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

            SetConnectionSettings(settings);

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

        private static Settings GetConnectionSettings()
        {
            try
            {
                string json = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.config"));
                return JsonSerializer.Parse<Settings>(json);
            }
            catch 
            {
                return new Settings();
            }
        }

        private static void SetConnectionSettings(Settings connectionSettings)
        {
            try
            {
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.config"),
                    JsonSerializer.ToString(connectionSettings));
            }
            catch
            {
                // ignored
            }
        }
    }
}

