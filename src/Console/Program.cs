using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Console.Configuration;
using log4net;
using log4net.Config;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Model.ReplyData;
using Terminal.Gui;

namespace Console
{
    internal static class Program
    {
        private static readonly ControlPanel ControlPanel = new ControlPanel();
        private static readonly Queue<string> Messages = new Queue<string>();
        private static readonly object MessageLock =  new object();

        private static Window _window;
        private static MenuBar _menuBar;

        private static void Main()
        {
            XmlConfigurator.Configure(
                LogManager.GetRepository(Assembly.GetAssembly(typeof(LogManager))),
                new FileInfo("log4net.config"));

            var settings = GetConnectionSettings();

            Guid id = ControlPanel.StartConnection(new SerialPortOsdpConnection(
                settings.ConnectionSettings.PortName,
                settings.ConnectionSettings.BaudRate));
            ControlPanel.AddDevice(id, 1, true);

            Application.Init();
            
            _window = new Window("OSDP.Net")
            {
                X = 0,
                Y = 1, // Leave one row for the toplevel menu

                Width = Dim.Fill(),
                Height = Dim.Fill() - 1
            };
            Application.Top.Add(_window);
            
            _menuBar = new MenuBar(new[]
            {
                new MenuBarItem("_File", new[]
                {
                    new MenuItem("_Quit", "", Application.RequestStop)
                }),
                new MenuBarItem("_Command", new[]
                {
                    new MenuItem("_ID Report", "", async () =>
                    {
                        DeviceIdentification deviceIdentification;
                        try
                        {
                            deviceIdentification = await ControlPanel.IdReport(id, 1);
                        }
                        catch (Exception exception)
                        {
                            
                        }
                    })
                })
            });
            
            Application.Top.Add (_menuBar);

            Application.Run();

            SetConnectionSettings(settings);

            ControlPanel.Shutdown();
        }

        public static void AddLogMessage(string message)
        {
            lock (MessageLock)
            {
                Messages.Enqueue(message);
                while (Messages.Count > 100)
                {
                    Messages.Dequeue();
                }

                // MessageView.Text = string.Join("", Messages.ToArray());
            }
        }

        private static Settings GetConnectionSettings()
        {
            try
            {
                string json = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.config"));
                return JsonSerializer.Deserialize<Settings>(json);
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
                    JsonSerializer.Serialize(connectionSettings));
            }
            catch
            {
                // ignored
            }
        }
    }
}

