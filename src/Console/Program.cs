using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Console.Commands;
using Console.Configuration;
using log4net;
using log4net.Config;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Messages;
using OSDP.Net.Model.CommandData;
using Terminal.Gui;

namespace Console
{
    internal static class Program
    {
        private static readonly ControlPanel ControlPanel = new ControlPanel();
        private static readonly Queue<string> Messages = new Queue<string>();
        private static readonly object MessageLock = new object();

        private static readonly MenuBarItem DevicesMenuBarItem =
            new MenuBarItem("_Devices", new[]
            {
                new MenuItem("_Add", string.Empty, AddDevice),
                new MenuItem("_Remove", string.Empty, RemoveDevice)
            });

        private static Guid _connectionId = Guid.Empty;
        private static Window _window;
        private static ScrollView _scrollView;
        private static MenuBar _menuBar;
        private static ControlPanel.NakReplyEventArgs _lastNak;

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

            _menuBar = new MenuBar(new[]
            {
                new MenuBarItem("_System", new[]
                {
                    new MenuItem("Save _Configuration", "", () => SetConnectionSettings(_settings)),
                    new MenuItem("_Quit", "", () =>
                    {
                        if (MessageBox.Query(40, 10, "Quit", "Quit program?", "Yes", "No") == 0)
                        {
                            Application.RequestStop();
                        }
                    })
                }),
                new MenuBarItem("Co_nnections", new[]
                {
                    new MenuItem("Start Serial Connection", "", StartSerialConnection),
                    new MenuItem("Start TCP Server Connection", "", StartTcpServerConnection),
                    new MenuItem("Start TCP Client Connection", "", StartTcpClientConnection),
                    new MenuItem("Stop Connections", "", ControlPanel.Shutdown),
                }),
                DevicesMenuBarItem,
                new MenuBarItem("_Commands", new[]
                {
                    new MenuItem("Communication Configuration", "", SendCommunicationConfiguration), 
                    new MenuItem("_Device Capabilities", "",
                        () => SendCommand("Device capabilities", _connectionId, ControlPanel.DeviceCapabilities)),
                    new MenuItem("_ID Report", "",
                        () => SendCommand("ID report", _connectionId, ControlPanel.IdReport)),
                    new MenuItem("Input Status", "",
                        () => SendCommand("Input status", _connectionId, ControlPanel.InputStatus)),
                    new MenuItem("_Local Status", "",
                        () => SendCommand("Local status", _connectionId, ControlPanel.LocalStatus)),
                    new MenuItem("Output Control", "", SendOutputControlCommand),
                    new MenuItem("Output Status", "",
                        () => SendCommand("Output status", _connectionId, ControlPanel.OutputStatus)),
                    new MenuItem("Reader Buzzer Control", "", () => ControlPanel.ReaderBuzzerControl(_connectionId, 0,
                        new ReaderBuzzerControl(0, ToneCode.Default, 10, 10, 4))),
                    new MenuItem("Reader LED Control", "", () => ControlPanel.ReaderLedControl(_connectionId, 0,
                        new ReaderLedControls(new[]
                        {
                            new ReaderLedControl(0, 0, TemporaryReaderControlCode.SetTemporaryAndStartTimer, 10, 10,
                                LedColor.Red, LedColor.Green, 100,
                                PermanentReaderControlCode.Nop, 0, 0, LedColor.Red, LedColor.Black),
                        }))),
                    new MenuItem("_Reader Status", "",
                        () => SendCommand("Reader status", _connectionId, ControlPanel.ReaderStatus))

                }),
                new MenuBarItem("_Invalid Commands", new[]
                {
                    new MenuItem("_Bad CRC/Checksum", "",
                        () => SendCustomCommand("Bad CRC/Checksum", _connectionId, ControlPanel.SendCustomCommand,
                            address => new InvalidCrcPollCommand(address)))
                })
            });

            Application.Top.Add(_menuBar, _window);


            _scrollView = new ScrollView(new Rect(0, 0, 0, 0))
            {
                ContentSize = new Size(500, 100),
                ShowVerticalScrollIndicator = true,
                ShowHorizontalScrollIndicator = true
            };
            _window.Add(_scrollView);
            
            RegisterEvents();

            Application.Run();

            ControlPanel.Shutdown();
        }

        private static void RegisterEvents()
        {
            ControlPanel.ConnectionStatusChanged += (sender, args) =>
            {
                DisplayReceivedReply($"Device '{_settings.Devices.Single(device => device.Address == args.Address).Name}' " +
                                     $"at address {args.Address} is now {(args.IsConnected ? "connected" : "disconnected")}",
                    string.Empty);
            };
            ControlPanel.NakReplyReceived += (sender, args) =>
            {
                var lastNak = _lastNak;
                _lastNak = args;
                if (lastNak != null && lastNak.Address == args.Address &&
                    lastNak.Nak.ErrorCode == args.Nak.ErrorCode)
                {
                    return;
                }

                AddLogMessage($"!!! Received NAK reply for address {args.Address} !!!{Environment.NewLine}{args.Nak}");
            };
            ControlPanel.LocalStatusReportReplyReceived += (sender, args) =>
            {
                DisplayReceivedReply($"Local status updated for address {args.Address}",
                    args.LocalStatus.ToString());
            };
            ControlPanel.InputStatusReportReplyReceived += (sender, args) =>
            {
                DisplayReceivedReply($"Input status updated for address {args.Address}",
                    args.InputStatus.ToString());
            };
            ControlPanel.OutputStatusReportReplyReceived += (sender, args) =>
            {
                DisplayReceivedReply($"Output status updated for address {args.Address}",
                    args.OutputStatus.ToString());
            };
            ControlPanel.ReaderStatusReportReplyReceived += (sender, args) =>
            {
                DisplayReceivedReply($"Reader tamper status updated for address {args.Address}",
                    args.ReaderStatus.ToString());
            };
            ControlPanel.RawCardDataReplyReceived += (sender, args) =>
            {
                DisplayReceivedReply($"Received raw card data reply for address {args.Address}",
                    args.RawCardData.ToString());
            };
        }

        private static void StartSerialConnection()
        {
            var portNameTextField = new TextField(15, 1, 35, _settings.SerialConnectionSettings.PortName);
            var baudRateTextField = new TextField(15, 3, 35, _settings.SerialConnectionSettings.BaudRate.ToString());

            void StartConnectionButtonClicked()
            {
                _settings.SerialConnectionSettings.PortName = portNameTextField.Text.ToString();
                if (!int.TryParse(baudRateTextField.Text.ToString(), out var baudRate))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid baud rate entered!", "OK");
                    return;
                }

                _settings.SerialConnectionSettings.BaudRate = baudRate;
                
                StartConnection(new SerialPortOsdpConnection(_settings.SerialConnectionSettings.PortName,
                    _settings.SerialConnectionSettings.BaudRate));
                
                Application.RequestStop();
            }
            
            Application.Run(new Dialog("Start Serial Connection", 60, 10,
                new Button("Start") {Clicked = StartConnectionButtonClicked},
                new Button("Cancel") {Clicked = Application.RequestStop})
            {
                new Label(1, 1, "Port:"),
                portNameTextField,
                new Label(1, 3, "Baud Rate:"),
                baudRateTextField
            });
        }

        private static void StartTcpServerConnection()
        {
            var portNumberTextField = new TextField(15, 1, 35, _settings.TcpServerConnectionSettings.PortNumber.ToString());
            var baudRateTextField = new TextField(15, 3, 35, _settings.TcpServerConnectionSettings.BaudRate.ToString());

            void StartConnectionButtonClicked()
            {
                if (!int.TryParse(portNumberTextField.Text.ToString(), out var portNumber))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid port number entered!", "OK");
                    return;
                }
                _settings.TcpServerConnectionSettings.PortNumber = portNumber;
                
                if (!int.TryParse(baudRateTextField.Text.ToString(), out var baudRate))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid baud rate entered!", "OK");
                    return;
                }
                _settings.TcpServerConnectionSettings.BaudRate = baudRate;
                
                StartConnection( new TcpServerOsdpConnection(_settings.TcpServerConnectionSettings.BaudRate = portNumber,
                    _settings.TcpServerConnectionSettings.BaudRate));
                
                Application.RequestStop();
            }
            
            Application.Run(new Dialog("Start TCP Server Connection", 60, 10,
                new Button("Start") {Clicked = StartConnectionButtonClicked},
                new Button("Cancel") {Clicked = Application.RequestStop})
            {
                new Label(1, 1, "Port Number:"),
                portNumberTextField,
                new Label(1, 3, "Baud Rate:"),
                baudRateTextField
            });
        }

        private static void StartTcpClientConnection()
        {
            var hostTextField = new TextField(15, 1, 35, _settings.TcpClientConnectionSettings.Host);
            var portNumberTextField =
                new TextField(15, 3, 35, _settings.TcpClientConnectionSettings.PortNumber.ToString());
            var baudRateTextField = new TextField(15, 5, 35, _settings.TcpClientConnectionSettings.BaudRate.ToString());

            void StartConnectionButtonClicked()
            {
                _settings.TcpClientConnectionSettings.Host = hostTextField.Text.ToString();

                if (!int.TryParse(portNumberTextField.Text.ToString(), out var portNumber))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid port number entered!", "OK");
                    return;
                }

                _settings.TcpClientConnectionSettings.PortNumber = portNumber;

                if (!int.TryParse(baudRateTextField.Text.ToString(), out var baudRate))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid baud rate entered!", "OK");
                    return;
                }

                _settings.TcpClientConnectionSettings.BaudRate = baudRate;
                
                StartConnection(new TcpClientOsdpConnection(
                    _settings.TcpClientConnectionSettings.Host,
                    _settings.TcpClientConnectionSettings.PortNumber,
                    _settings.TcpClientConnectionSettings.BaudRate));

                Application.RequestStop();
            }

            Application.Run(new Dialog("Start TCP Client Connection", 60, 13,
                new Button("Start") {Clicked = StartConnectionButtonClicked},
                new Button("Cancel") {Clicked = Application.RequestStop})
            {
                new Label(1, 1, "Host Name:"),
                hostTextField,
                new Label(1, 3, "Port Number:"),
                portNumberTextField,
                new Label(1, 5, "Baud Rate:"),
                baudRateTextField
            });
        }

        private static void StartConnection(IOsdpConnection osdpConnection)
        {
            ControlPanel.Shutdown();

            _connectionId = ControlPanel.StartConnection(osdpConnection);

            foreach (var device in _settings.Devices)
            {
                ControlPanel.AddDevice(_connectionId, device.Address, device.UseCrc, device.UseSecureChannel,
                    device.SecureChannelKey);
            }
        }

        private static void DisplayReceivedReply(string title, string message)
        {
            AddLogMessage($"{title}{Environment.NewLine}{message}{Environment.NewLine}{new string('*', 30)}");
        }

        public static void AddLogMessage(string message)
        {
            Application.MainLoop.Invoke(() =>
            {
                lock (MessageLock)
                {
                    Messages.Enqueue(message);
                    while (Messages.Count > 100)
                    {
                        Messages.Dequeue();
                    }

                    while (!_window.HasFocus)
                    {
                        return;
                    }

                    _scrollView.Frame = new Rect(1, 0, _window.Frame.Width - 3, _window.Frame.Height - 2);
                    _scrollView.RemoveAll();

                    int index = 0;
                    foreach (string outputMessage in Messages.Reverse())
                    {
                        var label = new Label(0, index,
                            outputMessage.Substring(0, outputMessage.Length - 1));

                        index += outputMessage.Length - outputMessage.Replace(Environment.NewLine, string.Empty).Length;

                        if (outputMessage.Contains("| WARN |") || outputMessage.Contains("NAK"))
                        {
                            label.TextColor = Terminal.Gui.Attribute.Make(Color.Black, Color.BrightYellow);
                        }

                        if (outputMessage.Contains("| ERROR |"))
                        {
                            label.TextColor = Terminal.Gui.Attribute.Make(Color.White, Color.BrightRed);
                        }

                        _scrollView.Add(label);
                    }
                }
            });
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

        private static void AddDevice()
        {
            if (_connectionId == Guid.Empty)
            {
                MessageBox.ErrorQuery(60, 10, "Information", "Start a connection before adding devices.", "OK");
                return;
            }
            
            var nameTextField = new TextField(15, 1, 35, string.Empty);
            var addressTextField = new TextField(15, 3, 35, string.Empty);
            var useCrcCheckBox = new CheckBox(1, 5, "Use CRC", true);
            var useSecureChannelCheckBox = new CheckBox(1, 6, "Use Secure Channel", true);

            void AddDeviceButtonClicked()
            {
                if (!byte.TryParse(addressTextField.Text.ToString(), out var address))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid address entered!", "OK");
                    return;
                }

                if (_settings.Devices.Any(device => device.Address == address))
                {
                    if (MessageBox.Query(60, 10, "Overwrite", "Device already exists at that address, overwrite?",
                            "Yes", "No") == 1)
                    {
                        return;
                    }
                }

                ControlPanel.AddDevice(_connectionId, address, useCrcCheckBox.Checked,
                    useSecureChannelCheckBox.Checked);

                var foundDevice = _settings.Devices.FirstOrDefault(device => device.Address == address);
                if (foundDevice != null)
                {
                    _settings.Devices.Remove(foundDevice);
                }

                _settings.Devices.Add(new DeviceSetting
                {
                    Address = address, Name = nameTextField.Text.ToString(),
                    UseSecureChannel = useSecureChannelCheckBox.Checked,
                    UseCrc = useCrcCheckBox.Checked
                });
                
                Application.RequestStop();
            }

            Application.Run(new Dialog("Add Device", 60, 13,
                new Button("Add") {Clicked = AddDeviceButtonClicked},
                new Button("Cancel") {Clicked = Application.RequestStop})
            {
                new Label(1, 1, "Name:"),
                nameTextField,
                new Label(1, 3, "Address:"),
                addressTextField,
                useCrcCheckBox,
                useSecureChannelCheckBox
            });
        }

        private static void RemoveDevice()
        {
            if (_connectionId == Guid.Empty)
            {
                MessageBox.ErrorQuery(60, 10, "Information", "Start a connection before removing devices.", "OK");
                return;
            }
            
            var orderedDevices = _settings.Devices.OrderBy(device => device.Address).ToArray();
            var scrollView = new ScrollView(new Rect(6, 1, 40, 6))
            {
                ContentSize = new Size(50, orderedDevices.Length * 2),
                ShowVerticalScrollIndicator = orderedDevices.Length > 6,
                ShowHorizontalScrollIndicator = false
            };

            var deviceRadioGroup = new RadioGroup(0, 0,
                orderedDevices.Select(device => $"{device.Address} : {device.Name}").ToArray());
            scrollView.Add(deviceRadioGroup);
            
            void RemoveDeviceButtonClicked()
            {
                var removedDevice = orderedDevices[deviceRadioGroup.Selected];
                ControlPanel.RemoveDevice(_connectionId, removedDevice.Address);
                _settings.Devices.Remove(removedDevice);
                Application.RequestStop();
            }

            Application.Run(new Dialog("Remove Device", 60, 13,
                new Button("Remove") {Clicked = RemoveDeviceButtonClicked},
                new Button("Cancel") {Clicked = Application.RequestStop})
            {
                scrollView
            });
        }

        private static void SendCommunicationConfiguration()
        {
            var addressTextField = new TextField(20, 1, 20,
                ((_settings.Devices.OrderBy(device => device.Address).LastOrDefault()?.Address ?? 0) + 1).ToString());
            var baudRateTextField = new TextField(20, 3, 20, _settings.SerialConnectionSettings.BaudRate.ToString());

            void StartConnectionButtonClicked()
            {
                if (!byte.TryParse(addressTextField.Text.ToString(), out var updatedAddress))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid updated address entered!", "OK");
                    return;
                }

                if (!int.TryParse(baudRateTextField.Text.ToString(), out var updatedBaudRate))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid updated baud rate entered!", "OK");
                    return;
                }

                SendCommand("Communication Configuration", _connectionId,
                    new CommunicationConfiguration(updatedAddress, updatedBaudRate),
                    ControlPanel.CommunicationConfiguration,
                    (address, configuration) =>
                    {
                        ControlPanel.RemoveDevice(_connectionId, address);

                        var updatedDevice = _settings.Devices.First(device => device.Address == address);
                        updatedDevice.Address = configuration.Address;
                        ControlPanel.AddDevice(_connectionId, updatedDevice.Address, updatedDevice.UseCrc,
                            updatedDevice.UseSecureChannel, updatedDevice.SecureChannelKey);
                    });

                Application.RequestStop();
            }

            Application.Run(new Dialog("Send Communication Configuration Command", 60, 10,
                new Button("Send") {Clicked = StartConnectionButtonClicked},
                new Button("Cancel") {Clicked = Application.RequestStop})
            {
                new Label(1, 1, "Updated Address:"),
                addressTextField,
                new Label(1, 3, "Updated Baud Rate:"),
                baudRateTextField
            });
        }

        private static void SendOutputControlCommand()
        {
            var outputAddressTextField = new TextField(20, 1, 20, "0");
            var activateOutputCheckBox = new CheckBox(15, 3, "Activate Output", false);

            void StartOutputControlButtonClicked()
            {
                if (!byte.TryParse(outputAddressTextField.Text.ToString(), out var outputAddress))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid output address entered!", "OK");
                    return;
                }

                SendCommand("Output Control Command", _connectionId, new OutputControls(new[]
                {
                    new OutputControl(outputAddress, activateOutputCheckBox.Checked
                        ? OutputControlCode.PermanentStateOnAbortTimedOperation
                        : OutputControlCode.PermanentStateOffAbortTimedOperation, 0)
                }), ControlPanel.OutputControl, (address, result) => { });

                Application.RequestStop();
            }

            Application.Run(new Dialog("Send Output Control Output Command", 60, 10,
                new Button("Send") {Clicked = StartOutputControlButtonClicked},
                new Button("Cancel") {Clicked = Application.RequestStop})
            {
                new Label(1, 1, "Output Address:"),
                outputAddressTextField,
                activateOutputCheckBox
            });
        }

        private static void SendCommand<T>(string title, Guid connectionId, Func<Guid, byte, Task<T>> sendCommandFunction)
        {
            if (_connectionId == Guid.Empty)
            {
                MessageBox.ErrorQuery(60, 10, "Information", "Start a connection before sending commands.", "OK");
                return;
            }
            
            var deviceSelectionView = CreateDeviceSelectionView(out var orderedDevices, out var deviceRadioGroup);

            void SendCommandButtonClicked()
            {
                var selectedDevice = orderedDevices[deviceRadioGroup.Selected];
                byte address = selectedDevice.Address;
                Application.RequestStop();

                Task.Run(async () =>
                {
                    try
                    {
                        var result = await sendCommandFunction(connectionId, address);
                        AddLogMessage($"{title} for address {address}{Environment.NewLine}{result}{Environment.NewLine}{new string('*', 30)}");
                    }
                    catch (Exception exception)
                    {
                        Application.MainLoop.Invoke(() =>
                        {
                            MessageBox.ErrorQuery(40, 10, $"Error on address {address}", exception.Message,
                                "OK");
                        });
                    }
                });
            }

            Application.Run(new Dialog(title, 60, 13,
                new Button("Send") {Clicked = SendCommandButtonClicked
                },
                new Button("Cancel") {Clicked = Application.RequestStop})
            {
                deviceSelectionView
            });
        }

        private static void SendCommand<T, TU>(string title, Guid connectionId, TU commandData,
            Func<Guid, byte, TU, Task<T>> sendCommandFunction, Action<byte, T> handleResult)
        {
            if (_connectionId == Guid.Empty)
            {
                MessageBox.ErrorQuery(60, 10, "Information", "Start a connection before sending commands.", "OK");
                return;
            }
            
            var deviceSelectionView = CreateDeviceSelectionView(out var orderedDevices, out var deviceRadioGroup);

            void SendCommandButtonClicked()
            {
                var selectedDevice = orderedDevices[deviceRadioGroup.Selected];
                byte address = selectedDevice.Address;
                Application.RequestStop();

                Task.Run(async () =>
                {
                    try
                    {
                        var result = await sendCommandFunction(connectionId, address, commandData);
                        AddLogMessage(
                            $"{title} for address {address}{Environment.NewLine}{result}{Environment.NewLine}{new string('*', 30)}");
                        handleResult(address, result);
                    }
                    catch (Exception exception)
                    {
                        Application.MainLoop.Invoke(() =>
                        {
                            MessageBox.ErrorQuery(40, 10, $"Error on address {address}", exception.Message,
                                "OK");
                        });
                    }
                });
            }

            Application.Run(new Dialog(title, 60, 13,
                new Button("Send")
                {
                    Clicked = SendCommandButtonClicked
                },
                new Button("Cancel") {Clicked = Application.RequestStop})
            {
                deviceSelectionView
            });
        }

        private static void SendCustomCommand(string title, Guid connectionId, Func<Guid, Command, Task> sendCommandFunction, Func<byte, Command> createCommand)
        {
            if (_connectionId == Guid.Empty)
            {
                MessageBox.ErrorQuery(60, 10, "Information", "Start a connection before sending commands.", "OK");
                return;
            }
            
            var deviceSelectionView = CreateDeviceSelectionView(out var orderedDevices, out var deviceRadioGroup);

            void SendCommandButtonClicked()
            {
                var selectedDevice = orderedDevices[deviceRadioGroup.Selected];
                byte address = selectedDevice.Address;
                Application.RequestStop();
                
                Task.Run(async () =>
                {
                    try
                    {
                        await sendCommandFunction(connectionId, createCommand(address));
                    }
                    catch (Exception exception)
                    {
                        Application.MainLoop.Invoke(() =>
                        {
                            MessageBox.ErrorQuery(40, 10, $"Error on address {address}", exception.Message,
                                "OK");
                        });
                    }
                });
            }

            Application.Run(new Dialog(title, 60, 13,
                new Button("Send") {Clicked = SendCommandButtonClicked
                },
                new Button("Cancel") {Clicked = Application.RequestStop})
            {
                deviceSelectionView
            });
        }

        private static ScrollView CreateDeviceSelectionView(out DeviceSetting[] orderedDevices, out RadioGroup deviceRadioGroup)
        {
            orderedDevices = _settings.Devices.OrderBy(device => device.Address).ToArray();
            var scrollView = new ScrollView(new Rect(6, 1, 40, 6))
            {
                ContentSize = new Size(50, orderedDevices.Length * 2),
                ShowVerticalScrollIndicator = orderedDevices.Length > 6,
                ShowHorizontalScrollIndicator = false
            };

            deviceRadioGroup = new RadioGroup(0, 0,
                orderedDevices.Select(device => $"{device.Address} : {device.Name}").ToArray());
            scrollView.Add(deviceRadioGroup);
            return scrollView;
        }
    }
}

