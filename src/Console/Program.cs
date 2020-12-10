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
using Microsoft.Extensions.Logging;
using NStack;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Messages;
using OSDP.Net.Model.CommandData;
using Terminal.Gui;

namespace Console
{
    internal static class Program
    {
        private static ControlPanel _controlPanel;
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
            
            var factory = new LoggerFactory();
            factory.AddLog4Net();
            
            _controlPanel = new ControlPanel(factory.CreateLogger<ControlPanel>());

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
                    new MenuItem("Stop Connections", "", _controlPanel.Shutdown),
                }),
                DevicesMenuBarItem,
                new MenuBarItem("_Commands", new[]
                {
                    new MenuItem("Communication Configuration", "", SendCommunicationConfiguration), 
                    new MenuItem("_Device Capabilities", "",
                        () => SendCommand("Device capabilities", _connectionId, _controlPanel.DeviceCapabilities)),
                    new MenuItem("_ID Report", "",
                        () => SendCommand("ID report", _connectionId, _controlPanel.IdReport)),
                    new MenuItem("Input Status", "",
                        () => SendCommand("Input status", _connectionId, _controlPanel.InputStatus)),
                    new MenuItem("_Local Status", "",
                        () => SendCommand("Local status", _connectionId, _controlPanel.LocalStatus)),
                    new MenuItem("Manufacturer Specific", "", SendManufacturerSpecificCommand),
                    new MenuItem("Output Control", "", SendOutputControlCommand),
                    new MenuItem("Output Status", "",
                        () => SendCommand("Output status", _connectionId, _controlPanel.OutputStatus)),
                    new MenuItem("Reader Buzzer Control", "", SendReaderBuzzerControlCommand),
                    new MenuItem("Reader LED Control", "", SendReaderLedControlCommand),
                    new MenuItem("Reader Text Output", "", SendReaderTextOutputCommand),
                    new MenuItem("_Reader Status", "",
                        () => SendCommand("Reader status", _connectionId, _controlPanel.ReaderStatus))

                }),
                new MenuBarItem("_Invalid Commands", new[]
                {
                    new MenuItem("_Bad CRC/Checksum", "",
                        () => SendCustomCommand("Bad CRC/Checksum", _connectionId, _controlPanel.SendCustomCommand,
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

            _controlPanel.Shutdown();
        }

        private static void RegisterEvents()
        {
            _controlPanel.ConnectionStatusChanged += (sender, args) =>
            {
                DisplayReceivedReply($"Device '{_settings.Devices.Single(device => device.Address == args.Address).Name}' " +
                                     $"at address {args.Address} is now {(args.IsConnected ? "connected" : "disconnected")}",
                    string.Empty);
            };
            _controlPanel.NakReplyReceived += (sender, args) =>
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
            _controlPanel.LocalStatusReportReplyReceived += (sender, args) =>
            {
                DisplayReceivedReply($"Local status updated for address {args.Address}",
                    args.LocalStatus.ToString());
            };
            _controlPanel.InputStatusReportReplyReceived += (sender, args) =>
            {
                DisplayReceivedReply($"Input status updated for address {args.Address}",
                    args.InputStatus.ToString());
            };
            _controlPanel.OutputStatusReportReplyReceived += (sender, args) =>
            {
                DisplayReceivedReply($"Output status updated for address {args.Address}",
                    args.OutputStatus.ToString());
            };
            _controlPanel.ReaderStatusReportReplyReceived += (sender, args) =>
            {
                DisplayReceivedReply($"Reader tamper status updated for address {args.Address}",
                    args.ReaderStatus.ToString());
            };
            _controlPanel.RawCardDataReplyReceived += (sender, args) =>
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

            var startButton = new Button("Start");
            startButton.Clicked += StartConnectionButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += Application.RequestStop;
            Application.Run(new Dialog("Start Serial Connection", 60, 10,
                startButton, cancelButton)
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
            
            var startButton = new Button("Start");
            startButton.Clicked += StartConnectionButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += Application.RequestStop;
            Application.Run(new Dialog("Start TCP Server Connection", 60, 10,
                startButton, cancelButton)
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

            var startButton = new Button("Start");
            startButton.Clicked += StartConnectionButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += Application.RequestStop;
            Application.Run(new Dialog("Start TCP Client Connection", 60, 13, startButton, cancelButton)
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
            _controlPanel.Shutdown();

            _connectionId = _controlPanel.StartConnection(osdpConnection);

            foreach (var device in _settings.Devices)
            {
                _controlPanel.AddDevice(_connectionId, device.Address, device.UseCrc, device.UseSecureChannel,
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
                            label.ColorScheme = new ColorScheme
                                {Normal = Terminal.Gui.Attribute.Make(Color.Black, Color.BrightYellow)};
                        }

                        if (outputMessage.Contains("| ERROR |"))
                        {
                            label.ColorScheme = new ColorScheme
                                {Normal = Terminal.Gui.Attribute.Make(Color.White, Color.BrightRed)};
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

                _controlPanel.AddDevice(_connectionId, address, useCrcCheckBox.Checked,
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

            var addButton = new Button("Add");
            addButton.Clicked += AddDeviceButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += Application.RequestStop;
            Application.Run(new Dialog("Add Device", 60, 13, addButton, cancelButton)
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
            var scrollView = new ScrollView(new Rect(6, 1, 50, 6))
            {
                ContentSize = new Size(40, orderedDevices.Length * 2),
                ShowVerticalScrollIndicator = orderedDevices.Length > 6,
                ShowHorizontalScrollIndicator = false
            };
            
            var deviceRadioGroup = new RadioGroup(0, 0,
                orderedDevices.Select(device => ustring.Make($"{device.Address} : {device.Name}")).ToArray());
            scrollView.Add(deviceRadioGroup);

            void RemoveDeviceButtonClicked()
            {
                var removedDevice = orderedDevices[deviceRadioGroup.SelectedItem];
                _controlPanel.RemoveDevice(_connectionId, removedDevice.Address);
                _settings.Devices.Remove(removedDevice);
                Application.RequestStop();
            }

            var removeButton = new Button("Remove");
            removeButton.Clicked += RemoveDeviceButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += Application.RequestStop;
            Application.Run(new Dialog("Remove Device", 60, 13, removeButton, cancelButton)
            {
                scrollView
            });
        }

        private static void SendCommunicationConfiguration()
        {
            var addressTextField = new TextField(20, 1, 20,
                ((_settings.Devices.OrderBy(device => device.Address).LastOrDefault()?.Address ?? 0) + 1).ToString());
            var baudRateTextField = new TextField(20, 3, 20, _settings.SerialConnectionSettings.BaudRate.ToString());

            void SendCommunictationConfigurationButtonClicked()
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
                    _controlPanel.CommunicationConfiguration,
                    (address, configuration) =>
                    {
                        _controlPanel.RemoveDevice(_connectionId, address);

                        var updatedDevice = _settings.Devices.First(device => device.Address == address);
                        updatedDevice.Address = configuration.Address;
                        _controlPanel.AddDevice(_connectionId, updatedDevice.Address, updatedDevice.UseCrc,
                            updatedDevice.UseSecureChannel, updatedDevice.SecureChannelKey);
                    });

                Application.RequestStop();
            }

            var sendButton = new Button("Send");
            sendButton.Clicked += SendCommunictationConfigurationButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += Application.RequestStop;
            Application.Run(new Dialog("Send Communication Configuration Command", 60, 10, sendButton, cancelButton)
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

            void SendOutputControlButtonClicked()
            {
                if (!byte.TryParse(outputAddressTextField.Text.ToString(), out var outputNumber))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid output address entered!", "OK");
                    return;
                }

                SendCommand("Output Control Command", _connectionId, new OutputControls(new[]
                {
                    new OutputControl(outputNumber, activateOutputCheckBox.Checked
                        ? OutputControlCode.PermanentStateOnAbortTimedOperation
                        : OutputControlCode.PermanentStateOffAbortTimedOperation, 0)
                }), _controlPanel.OutputControl, (address, result) => { });

                Application.RequestStop();
            }

            var sendButton = new Button("Send");
            sendButton.Clicked += SendOutputControlButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += Application.RequestStop;
            Application.Run(new Dialog("Send Output Control Command", 60, 10, sendButton, cancelButton)
            {
                new Label(1, 1, "Output Number:"),
                outputAddressTextField,
                activateOutputCheckBox
            });
        }

        private static void SendReaderLedControlCommand()
        {
            var readerAddressTextField = new TextField(20, 1, 20, "0");
            var colorTextField = new TextField(20, 3, 20, "Red");

            void SendReaderLedControlButtonClicked()
            {
                if (!byte.TryParse(readerAddressTextField.Text.ToString(), out var readerNumber))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid reader number entered!", "OK");
                    return;
                }

                if (!Enum.TryParse(colorTextField.Text.ToString(), out LedColor color))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid LED color entered!", "OK");
                    return;
                }

                SendCommand("Reader LED Control Command", _connectionId, new ReaderLedControls(new[]
                {
                    new ReaderLedControl(readerNumber, 0,
                        TemporaryReaderControlCode.CancelAnyTemporaryAndDisplayPermanent, 0, 0,
                        LedColor.Red, LedColor.Green, 0,
                        PermanentReaderControlCode.SetPermanentState, 0, 0, color, color)
                }), _controlPanel.ReaderLedControl, (address, result) => { });

                Application.RequestStop();
            }

            var sendButton = new Button("Send");
            sendButton.Clicked += SendReaderLedControlButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += Application.RequestStop;
            Application.Run(new Dialog("Send Reader LED Control Command", 60, 10, sendButton, cancelButton)
            {
                new Label(1, 1, "Reader Number:"),
                readerAddressTextField,
                new Label(1, 3, "Color:"),
                colorTextField
            });
        }

        private static void SendManufacturerSpecificCommand()
        {
            var vendorCodeTextField = new TextField(20, 1, 20, string.Empty);
            var dataTextField = new TextField(20, 3, 20, string.Empty);

            void SendOutputControlButtonClicked()
            {
                byte[] vendorCode;
                try
                {
                    vendorCode = Convert.FromBase64String(vendorCodeTextField.Text.ToString() ?? string.Empty);
                }
                catch
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid vendor code entered!", "OK");
                    return;
                }

                if (vendorCode.Length != 3)
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Vendor code needs to be 3 bytes!", "OK");
                    return;
                }

                byte[] data;
                try
                {
                    data = Convert.FromBase64String(dataTextField.Text.ToString() ?? string.Empty);
                }
                catch
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid data entered!", "OK");
                    return;
                }

                SendCommand("Manufacturer Specific Command", _connectionId,
                    new ManufacturerSpecificCommandData(vendorCode.ToArray(), data.ToArray()),
                    _controlPanel.ManufacturerSpecificCommand, (b, b1) => { });

                Application.RequestStop();
            }

            var sendButton = new Button("Send");
            sendButton.Clicked += SendOutputControlButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += Application.RequestStop;
            Application.Run(new Dialog("Send Manufacturer Specific Command (Enter Base64)", 60, 10, sendButton, cancelButton)
            {
                new Label(1, 1, "Vendor Code:"),
                vendorCodeTextField,
                new Label(1, 3, "Data:"),
                dataTextField
            });
        }

        private static void SendReaderBuzzerControlCommand()
        {
            var readerAddressTextField = new TextField(20, 1, 20, "0");
            var repeatTimesTextField = new TextField(20, 3, 20, "1");

            void SendReaderBuzzerControlButtonClicked()
            {
                if (!byte.TryParse(readerAddressTextField.Text.ToString(), out byte readerNumber))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid reader number entered!", "OK");
                    return;
                }

                if (!byte.TryParse(repeatTimesTextField.Text.ToString(), out byte repeatNumber))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid repeat number entered!", "OK");
                    return;
                }

                SendCommand("Reader Buzzer Control Command", _connectionId,
                    new ReaderBuzzerControl(readerNumber, ToneCode.Default, 2, 2, repeatNumber),
                    _controlPanel.ReaderBuzzerControl, (address, result) => { });

                Application.RequestStop();
            }

            var sendButton = new Button("Send");
            sendButton.Clicked += SendReaderBuzzerControlButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += Application.RequestStop;
            Application.Run(new Dialog("Send Reader Buzzer Control Command", 60, 10, sendButton, cancelButton)
            {
                new Label(1, 1, "Reader Number:"),
                readerAddressTextField,
                new Label(1, 3, "Repeat Times:"),
                repeatTimesTextField
            });
        }

        private static void SendReaderTextOutputCommand()
        {
            var readerAddressTextField = new TextField(20, 1, 20, "0");
            var textOutputTextField = new TextField(20, 3, 20, "Some Text");

            void SendReaderTextOutputButtonClicked()
            {
                if (!byte.TryParse(readerAddressTextField.Text.ToString(), out byte readerNumber))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid reader number entered!", "OK");
                    return;
                }

                SendCommand("Reader Text Output Command", _connectionId,
                    new ReaderTextOutput(readerNumber, TextCommand.PermanentTextNoWrap, 0, 1, 1,
                        textOutputTextField.Text.ToString()),
                    _controlPanel.ReaderTextOutput, (address, result) => { });

                Application.RequestStop();
            }

            var sendButton = new Button("Send");
            sendButton.Clicked += SendReaderTextOutputButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += Application.RequestStop;
            Application.Run(new Dialog("Reader Text Output Command", 60, 10, sendButton, cancelButton)
            {
                new Label(1, 1, "Reader Number:"),
                readerAddressTextField,
                new Label(1, 3, "Text Output:"),
                textOutputTextField
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
                var selectedDevice = orderedDevices[deviceRadioGroup.SelectedItem];
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

            var sendButton = new Button("Send");
            sendButton.Clicked += SendCommandButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += Application.RequestStop;
            Application.Run(new Dialog(title, 60, 13, sendButton, cancelButton)
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
                var selectedDevice = orderedDevices[deviceRadioGroup.SelectedItem];
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

            var sendButton = new Button("Send");
            sendButton.Clicked += SendCommandButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += Application.RequestStop;
            Application.Run(new Dialog(title, 60, 13, sendButton, cancelButton)
            {
                deviceSelectionView
            });
        }

        private static void SendCustomCommand(string title, Guid connectionId,
            Func<Guid, Command, Task> sendCommandFunction, Func<byte, Command> createCommand)
        {
            if (_connectionId == Guid.Empty)
            {
                MessageBox.ErrorQuery(60, 10, "Information", "Start a connection before sending commands.", "OK");
                return;
            }

            var deviceSelectionView = CreateDeviceSelectionView(out var orderedDevices, out var deviceRadioGroup);

            void SendCommandButtonClicked()
            {
                var selectedDevice = orderedDevices[deviceRadioGroup.SelectedItem];
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

            var sendButton = new Button("Send");
            sendButton.Clicked += SendCommandButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += Application.RequestStop;
            Application.Run(new Dialog(title, 60, 13, sendButton, cancelButton)
            {
                deviceSelectionView
            });
        }

        private static ScrollView CreateDeviceSelectionView(out DeviceSetting[] orderedDevices,
            out RadioGroup deviceRadioGroup)
        {
            orderedDevices = _settings.Devices.OrderBy(device => device.Address).ToArray();
            var scrollView = new ScrollView(new Rect(6, 1, 50, 6))
            {
                ContentSize = new Size(40, orderedDevices.Length * 2),
                ShowVerticalScrollIndicator = orderedDevices.Length > 6,
                ShowHorizontalScrollIndicator = false
            };

            deviceRadioGroup = new RadioGroup(0, 0,
                orderedDevices.Select(device => ustring.Make($"{device.Address} : {device.Name}")).ToArray());
            deviceRadioGroup.SelectedItem = 0;
            scrollView.Add(deviceRadioGroup);
            return scrollView;
        }
    }
}

