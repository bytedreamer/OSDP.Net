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

        private static Guid _connectionId;
        private static Window _window;
        private static MenuBar _menuBar;
        private static Settings _settings;

        private static void Main()
        {
            XmlConfigurator.Configure(
                LogManager.GetRepository(Assembly.GetAssembly(typeof(LogManager))),
                new FileInfo("log4net.config"));

            _settings = GetConnectionSettings();

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
                new MenuBarItem("_System", new[]
                {
                    new MenuItem("_Start Connection", "", StartConnection),
                    new MenuItem("_Stop Connection", "", ControlPanel.Shutdown),
                    new MenuItem("_Quit", "", Application.RequestStop)
                }),
                new MenuBarItem("_Configuration", new[]
                {
                    new MenuItem("_Save", "",() => SetConnectionSettings(_settings))
                }),
                new MenuBarItem("_Command", new[]
                {
                    new MenuItem("_ID Report", "", async () =>
                    {
                        DeviceIdentification deviceIdentification;
                        try
                        {
                            deviceIdentification = await ControlPanel.IdReport(_connectionId, 1);
                        }
                        catch (Exception exception)
                        {
                            
                        }
                    })
                })
            });
            
            Application.Top.Add (_menuBar);
            
            Application.Run();

            ControlPanel.Shutdown();
        }

        private static void StartConnection()
        {
            var portNameTextField = new TextField(15, 1, 35, _settings.ConnectionSettings.PortName);
            var baudRateTextField = new TextField(15, 3, 35, _settings.ConnectionSettings.BaudRate.ToString());

            void StartConnectionButtonClicked()
            {
                _settings.ConnectionSettings.PortName = portNameTextField.Text.ToString();
                if (!int.TryParse(baudRateTextField.Text.ToString(), out var baudRate))
                {
                    Application.Run(new Dialog("Error", 40, 10,
                            new Button("OK")
                            {
                                Clicked = Application.RequestStop
                            })
                    {
                        new Label(1, 1, "Invalid baud rate entered!")
                    });
                    return;
                }

                _settings.ConnectionSettings.BaudRate = baudRate;

                ControlPanel.Shutdown();
                _connectionId = ControlPanel.StartConnection(
                    new SerialPortOsdpConnection(_settings.ConnectionSettings.PortName,
                        _settings.ConnectionSettings.BaudRate));
                ControlPanel.AddDevice(_connectionId, 1, true);
                Application.RequestStop();
            }
            
            Application.Run(new Dialog("Start Connection", 60, 10,
                new Button("Start") {Clicked = StartConnectionButtonClicked},
                new Button("Cancel") {Clicked = Application.RequestStop})
            {
                new Label(1, 1, "Port:"),
                portNameTextField,
                new Label(1, 3, "Baud Rate:"),
                baudRateTextField
            });
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

