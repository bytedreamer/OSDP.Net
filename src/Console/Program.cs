using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Console.Commands;
using Console.Configuration;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Logging;
using NStack;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Model.ReplyData;
using OSDP.Net.PanelCommands.DeviceDiscover;
using Terminal.Gui;
using Command = OSDP.Net.Messages.Command;
using CommunicationConfiguration = OSDP.Net.Model.CommandData.CommunicationConfiguration;
using ManufacturerSpecific = OSDP.Net.Model.CommandData.ManufacturerSpecific;

namespace Console
{
    internal static class Program
    {
        private static ILogger<ControlPanel> _panelLogger;
        private static ControlPanel _controlPanel;
        private static readonly Queue<string> Messages = new ();
        private static readonly object MessageLock = new ();

        private static readonly MenuItem DiscoverMenuItem =
            new MenuItem("_Discover", string.Empty, DiscoverDevice);
        private static readonly MenuBarItem DevicesMenuBarItem =
            new ("_Devices", new[]
            {
                new MenuItem("_Add", string.Empty, AddDevice),
                new MenuItem("_Remove", string.Empty, RemoveDevice),
                DiscoverMenuItem,
            });

        private static Guid _connectionId = Guid.Empty;
        private static Window _window;
        private static ScrollView _scrollView;
        private static MenuBar _menuBar;
        private static readonly ConcurrentDictionary<byte, ControlPanel.NakReplyEventArgs> LastNak = new ();

        private static string _lastFilePath;
        private static Settings _settings;

        private static async Task Main()
        {
            XmlConfigurator.Configure(
                LogManager.GetRepository(Assembly.GetAssembly(typeof(LogManager))),
                new FileInfo("log4net.config"));

            _lastFilePath = Path.Combine(Environment.CurrentDirectory, "appsettings.config");
            
            var factory = new LoggerFactory();
            factory.AddLog4Net();
            _panelLogger = factory.CreateLogger<ControlPanel>();

            _controlPanel = new ControlPanel(_panelLogger);

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
                    new MenuItem("_About", "", () => MessageBox.Query(40, 6,"About",
                        $"Version: {Assembly.GetEntryAssembly()?.GetName().Version}",0, "OK")),
                    new MenuItem("_Connection Settings", "", UpdateConnectionSettings),
                    new MenuItem("_Load Configuration", "", () => LoadConfigurationSettings()),
                    new MenuItem("_Save Configuration", "", () => SaveConfigurationSettings(_settings)),
                    new MenuItem("_Quit", "", () =>
                    {
                        SaveConfigurationSettings(_settings);

                        Application.RequestStop();
                        
                        Environment.Exit(Environment.ExitCode);
                    })
                }),
                new MenuBarItem("Co_nnections", new[]
                {
                    new MenuItem("Start Serial Connection", "", StartSerialConnection),
                    new MenuItem("Start TCP Server Connection", "", StartTcpServerConnection),
                    new MenuItem("Start TCP Client Connection", "", StartTcpClientConnection),
                    new MenuItem("Stop Connections", "", () =>
                    {
                        _connectionId = Guid.Empty;
                        _ = _controlPanel.Shutdown();
                    })
                }),
                DevicesMenuBarItem,
                new MenuBarItem("_Commands", new[]
                {
                    new MenuItem("Communication Configuration", "", SendCommunicationConfiguration), 
                    new MenuItem("Biometric Read", "", SendBiometricReadCommand), 
                    new MenuItem("Biometric Match", "", SendBiometricMatchCommand), 
                    new MenuItem("_Device Capabilities", "",
                        () => SendCommand("Device capabilities", _connectionId, _controlPanel.DeviceCapabilities)),
                    new MenuItem("Encryption Key Set", "", SendEncryptionKeySetCommand),
                    new MenuItem("File Transfer", "", SendFileTransferCommand),
                    new MenuItem("_ID Report", "",
                        () => SendCommand("ID report", _connectionId, _controlPanel.IdReport)),
                    new MenuItem("Input Status", "",
                        () => SendCommand("Input status", _connectionId, _controlPanel.InputStatus)),
                    new MenuItem("_Local Status", "",
                        () => SendCommand("Local Status", _connectionId, _controlPanel.LocalStatus)),
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
                            address => new InvalidCrcPollCommand(address))),
                    new MenuItem("Invalid Command Length", "",
                    () => SendCustomCommand("Invalid Command Length", _connectionId, _controlPanel.SendCustomCommand,
                        address => new InvalidLengthPollCommand(address))),
                    new MenuItem("Invalid Command", "",
                    () => SendCustomCommand("Invalid Command Length", _connectionId, _controlPanel.SendCustomCommand,
                        address => new InvalidCommand(address)))
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

            await _controlPanel.Shutdown();
        }

        private static void RegisterEvents()
        {
            _controlPanel.ConnectionStatusChanged += (_, args) =>
            {
                DisplayReceivedReply(
                    $"Device '{_settings.Devices.SingleOrDefault(device => device.Address == args.Address, new DeviceSetting() { Name="[Unknown]"}).Name}' " +
                    $"at address {args.Address} is now " +
                    $"{(args.IsConnected ? (args.IsSecureChannelEstablished ? "connected with secure channel" : "connected with clear text") : "disconnected")}",
                    string.Empty);
            };
            _controlPanel.NakReplyReceived += (_, args) =>
            {
                LastNak.TryRemove(args.Address, out var lastNak);
                LastNak.TryAdd(args.Address, args);
                if (lastNak != null && lastNak.Address == args.Address &&
                    lastNak.Nak.ErrorCode == args.Nak.ErrorCode)
                {
                    return;
                }

                AddLogMessage($"!!! Received NAK reply for address {args.Address} !!!{Environment.NewLine}{args.Nak}");
            };
            _controlPanel.LocalStatusReportReplyReceived += (_, args) =>
            {
                DisplayReceivedReply($"Local status updated for address {args.Address}",
                    args.LocalStatus.ToString());
            };
            _controlPanel.InputStatusReportReplyReceived += (_, args) =>
            {
                DisplayReceivedReply($"Input status updated for address {args.Address}",
                    args.InputStatus.ToString());
            };
            _controlPanel.OutputStatusReportReplyReceived += (_, args) =>
            {
                DisplayReceivedReply($"Output status updated for address {args.Address}",
                    args.OutputStatus.ToString());
            };
            _controlPanel.ReaderStatusReportReplyReceived += (_, args) =>
            {
                DisplayReceivedReply($"Reader tamper status updated for address {args.Address}",
                    args.ReaderStatus.ToString());
            };
            _controlPanel.RawCardDataReplyReceived += (_, args) =>
            {
                DisplayReceivedReply($"Received raw card data reply for address {args.Address}",
                    args.RawCardData.ToString());
            };
            _controlPanel.KeypadReplyReceived += (_, args) =>
            {
                DisplayReceivedReply($"Received keypad data reply for address {args.Address}",
                    args.KeypadData.ToString());
            };
        }

        private static void StartSerialConnection()
        {
            var portNameComboBox = CreatePortNameComboBox(15, 1);
                
            var baudRateTextField = new TextField(25, 3, 25, _settings.SerialConnectionSettings.BaudRate.ToString());
            var replyTimeoutTextField =
                new TextField(25, 5, 25, _settings.SerialConnectionSettings.ReplyTimeout.ToString());

            async void StartConnectionButtonClicked()
            {
                if (string.IsNullOrEmpty(portNameComboBox.Text.ToString()))
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "No port name entered!", "OK");
                    return;
                }
                
                if (!int.TryParse(baudRateTextField.Text.ToString(), out var baudRate))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid baud rate entered!", "OK");
                    return;
                }

                if (!int.TryParse(replyTimeoutTextField.Text.ToString(), out var replyTimeout))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid reply timeout entered!", "OK");
                    return;
                }

                _settings.SerialConnectionSettings.PortName = portNameComboBox.Text.ToString();
                _settings.SerialConnectionSettings.BaudRate = baudRate;
                _settings.SerialConnectionSettings.ReplyTimeout = replyTimeout;

                await StartConnection(new SerialPortOsdpConnection(_settings.SerialConnectionSettings.PortName,
                        _settings.SerialConnectionSettings.BaudRate)
                    { ReplyTimeout = TimeSpan.FromMilliseconds(_settings.SerialConnectionSettings.ReplyTimeout) });

                Application.RequestStop();
            }

            var startButton = new Button("Start", true);
            startButton.Clicked += StartConnectionButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("Start Serial Connection", 70, 12,
                cancelButton, startButton);
            dialog.Add(new Label(1, 1, "Port:"),
                portNameComboBox,
                new Label(1, 3, "Baud Rate:"),
                baudRateTextField,
                new Label(1, 5, "Reply Timeout(ms):"),
                replyTimeoutTextField);
            portNameComboBox.SetFocus();

            Application.Run(dialog);
        }

        private static void StartTcpServerConnection()
        {
            var portNumberTextField =
                new TextField(25, 1, 25, _settings.TcpServerConnectionSettings.PortNumber.ToString());
            var baudRateTextField = new TextField(25, 3, 25, _settings.TcpServerConnectionSettings.BaudRate.ToString());
            var replyTimeoutTextField =
                new TextField(25, 5, 25, _settings.SerialConnectionSettings.ReplyTimeout.ToString());

            async void StartConnectionButtonClicked()
            {
                if (!int.TryParse(portNumberTextField.Text.ToString(), out var portNumber))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid port number entered!", "OK");
                    return;
                }

                if (!int.TryParse(baudRateTextField.Text.ToString(), out var baudRate))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid baud rate entered!", "OK");
                    return;
                }

                if (!int.TryParse(replyTimeoutTextField.Text.ToString(), out var replyTimeout))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid reply timeout entered!", "OK");
                    return;
                }

                _settings.TcpServerConnectionSettings.PortNumber = portNumber;
                _settings.TcpServerConnectionSettings.BaudRate = baudRate;
                _settings.TcpServerConnectionSettings.ReplyTimeout = replyTimeout;

                await StartConnection(new TcpServerOsdpConnection(_settings.TcpServerConnectionSettings.PortNumber,
                        _settings.TcpServerConnectionSettings.BaudRate)
                    { ReplyTimeout = TimeSpan.FromMilliseconds(_settings.TcpServerConnectionSettings.ReplyTimeout) });

                Application.RequestStop();
            }

            var startButton = new Button("Start", true);
            startButton.Clicked += StartConnectionButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("Start TCP Server Connection", 60, 12, cancelButton,
                startButton);
            dialog.Add(new Label(1, 1, "Port Number:"),
                portNumberTextField,
                new Label(1, 3, "Baud Rate:"),
                baudRateTextField,
                new Label(1, 5, "Reply Timeout(ms):"),
                replyTimeoutTextField);
            portNumberTextField.SetFocus();

            Application.Run(dialog);
        }

        private static void StartTcpClientConnection()
        {
            var hostTextField = new TextField(15, 1, 35, _settings.TcpClientConnectionSettings.Host);
            var portNumberTextField =
                new TextField(25, 3, 25, _settings.TcpClientConnectionSettings.PortNumber.ToString());
            var baudRateTextField = new TextField(25, 5, 25, _settings.TcpClientConnectionSettings.BaudRate.ToString());
            var replyTimeoutTextField = new TextField(25, 7, 25, _settings.SerialConnectionSettings.ReplyTimeout.ToString());

            async void StartConnectionButtonClicked()
            {
                if (!int.TryParse(portNumberTextField.Text.ToString(), out var portNumber))
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid port number entered!", "OK");
                    return;
                }

                if (!int.TryParse(baudRateTextField.Text.ToString(), out var baudRate))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid baud rate entered!", "OK");
                    return;
                }
                
                if (!int.TryParse(replyTimeoutTextField.Text.ToString(), out var replyTimeout))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid reply timeout entered!", "OK");
                    return;
                }

                _settings.TcpClientConnectionSettings.Host = hostTextField.Text.ToString();
                _settings.TcpClientConnectionSettings.BaudRate = baudRate;
                _settings.TcpClientConnectionSettings.PortNumber = portNumber;
                _settings.TcpClientConnectionSettings.ReplyTimeout = replyTimeout;

                await StartConnection(new TcpClientOsdpConnection(
                    _settings.TcpClientConnectionSettings.Host,
                    _settings.TcpClientConnectionSettings.PortNumber,
                    _settings.TcpClientConnectionSettings.BaudRate));

                Application.RequestStop();
            }

            var startButton = new Button("Start", true);
            startButton.Clicked += StartConnectionButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("Start TCP Client Connection", 60, 15, cancelButton, startButton);
            dialog.Add(new Label(1, 1, "Host Name:"),
                hostTextField,
                new Label(1, 3, "Port Number:"),
                portNumberTextField,
                new Label(1, 5, "Baud Rate:"),
                baudRateTextField,
                new Label(1, 7, "Reply Timeout(ms):"),
                replyTimeoutTextField);
            hostTextField.SetFocus();
            
            Application.Run(dialog);
        }

        private static void UpdateConnectionSettings()
        {
            var pollingIntervalTextField = new TextField(25, 4, 25, _settings.PollingInterval.ToString());
            var tracingCheckBox = new CheckBox(1, 6, "Write packet data to file", _settings.IsTracing);
            
            void UpdateConnectionSettingsButtonClicked()
            {
                if (!int.TryParse(pollingIntervalTextField.Text.ToString(), out var pollingInterval))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid polling interval entered!", "OK");
                    return;
                }

                _settings.PollingInterval = pollingInterval;
                _settings.IsTracing = tracingCheckBox.Checked;
                
                Application.RequestStop();
            }

            var updateButton = new Button("Update", true);
            updateButton.Clicked += UpdateConnectionSettingsButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("Update Connection Settings", 60, 12, cancelButton, updateButton);
            dialog.Add(new Label(new Rect(1, 1, 55, 2), "Connection will need to be restarted for setting to take effect."),
                new Label(1, 4, "Polling Interval(ms):"),
                pollingIntervalTextField,
                tracingCheckBox);
            pollingIntervalTextField.SetFocus();
            
            Application.Run(dialog);
        }

        private static async Task StartConnection(IOsdpConnection osdpConnection)
        {
            LastNak.Clear();

            if (_connectionId != Guid.Empty)
            {
                await _controlPanel.Shutdown();
            }

            _connectionId =
                _controlPanel.StartConnection(osdpConnection, TimeSpan.FromMilliseconds(_settings.PollingInterval),
                    _settings.IsTracing);

            foreach (var device in _settings.Devices)
            {
                _controlPanel.AddDevice(_connectionId, device.Address, device.UseCrc, device.UseSecureChannel,
                    device.SecureChannelKey);
            }
        }

        private static ComboBox CreatePortNameComboBox(int x, int y)
        {
            var portNames = SerialPort.GetPortNames();
            var portNameComboBox = new ComboBox(new Rect(x, y, 35, 5), portNames);

            // Select default port name
            if (portNames.Length > 0)
            {
                portNameComboBox.SelectedItem = Math.Max(
                    Array.FindIndex(portNames, (port) =>
                    String.Equals(port, _settings.SerialConnectionSettings.PortName)), 0);
            }

            return portNameComboBox;
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

                    // Not sure why this is here but it is. When the window is not focused, the client area will not
                    // get updated but when we return to the window it will also not be updated. And... if the user
                    // clicks on the menubar, that is also considered to be "outside" of the window. For now to make
                    // output updates work when user is navigating submenus, just adding _menuBar check here
                    // -- DXM 2022-11-03
                    // p.s. this was a while loop???
                    if (!_window.HasFocus && _menuBar.HasFocus)
                    {
                        return;
                    }

                    _scrollView.Frame = new Rect(1, 0, _window.Frame.Width - 3, _window.Frame.Height - 2);
                    foreach (var view in _scrollView.Subviews)
                    {
                        view.Dispose();
                    }
                    _scrollView.RemoveAll();

                    // This is one hell of an approach in this function. Every time we add a line, we nuke entire view
                    // and add a bunch of labels. Is it possible to use something like a TextView set to read-only here
                    // instead?
                    // -- DXM 2022-11-03

                    int index = 0;
                    foreach (string outputMessage in Messages.Reverse())
                    {
                        var label = new Label(0, index, outputMessage.TrimEnd());
                        index += label.Bounds.Height;

                        if (outputMessage.Contains("| WARN |") || outputMessage.Contains("NAK"))
                        {
                            label.ColorScheme = new ColorScheme
                                { Normal = Terminal.Gui.Attribute.Make(Color.Black, Color.BrightYellow) };
                        }

                        if (outputMessage.Contains("| ERROR |"))
                        {
                            label.ColorScheme = new ColorScheme
                                { Normal = Terminal.Gui.Attribute.Make(Color.White, Color.BrightRed) };
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

        private static void SaveConfigurationSettings(Settings connectionSettings)
        {
            var saveDialog = new SaveDialog("Save Configuration", string.Empty, new List<string>{".config"});
            saveDialog.DirectoryPath = ustring.Make(Path.GetDirectoryName(_lastFilePath));
            saveDialog.FilePath = ustring.Make(Path.GetFileName(_lastFilePath));
            Application.Run(saveDialog);
            
            string savedFilePath = saveDialog.FilePath?.ToString() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(savedFilePath) || saveDialog.Canceled) return;
            
            try
            {
                File.WriteAllText(savedFilePath,JsonSerializer.Serialize(connectionSettings));
                _lastFilePath = savedFilePath;
                MessageBox.Query(40, 6, "Save Configuration", "Save completed successfully", "OK");
            }
            catch (Exception exception)
            {
                MessageBox.ErrorQuery(40, 8, "Error", exception.Message, "OK");
            }
        }

        private static void LoadConfigurationSettings()
        {
            var openDialog = new OpenDialog("Load Configuration", string.Empty, new List<string>{".config"});
            openDialog.DirectoryPath = ustring.Make(Path.GetDirectoryName(_lastFilePath));
            openDialog.FilePath = ustring.Make(Path.GetFileName(_lastFilePath));
            
            Application.Run(openDialog);
            
            string openFilePath = openDialog.FilePath?.ToString() ?? string.Empty;
            
            if (openDialog.Canceled || !File.Exists(openFilePath)) return;
            
            try
            {
                string json = File.ReadAllText(openFilePath);
                _settings = JsonSerializer.Deserialize<Settings>(json);
                _lastFilePath = openFilePath;
                MessageBox.Query(40, 6, "Load Configuration", "Load completed successfully", "OK");
            }
            catch (Exception exception)
            {
                MessageBox.ErrorQuery(40, 8, "Error", exception.Message, "OK");
            }
        }

        private static void AddDevice()
        {
            if (_connectionId == Guid.Empty)
            {
                MessageBox.ErrorQuery(60, 12, "Information", "Start a connection before adding devices.", "OK");
                return;
            }

            var nameTextField = new TextField(15, 1, 35, string.Empty);
            var addressTextField = new TextField(15, 3, 35, string.Empty);
            var useCrcCheckBox = new CheckBox(1, 5, "Use CRC", true);
            var useSecureChannelCheckBox = new CheckBox(1, 6, "Use Secure Channel", true);
            var keyTextField = new TextField(15, 8, 35, Convert.ToHexString(DeviceSetting.DefaultKey));

            void AddDeviceButtonClicked()
            {
                if (!byte.TryParse(addressTextField.Text.ToString(), out var address) || address > 127)
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid address entered!", "OK");
                    return;
                }

                if (keyTextField.Text == null || keyTextField.Text.Length != 32)
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid key length entered!", "OK");
                    return;
                }

                byte[] key;
                try
                {
                    key = Convert.FromHexString(keyTextField.Text.ToString()!);
                }
                catch
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid hex characters!", "OK");
                    return;
                }

                if (_settings.Devices.Any(device => device.Address == address))
                {
                    if (MessageBox.Query(60, 10, "Overwrite", "Device already exists at that address, overwrite?", 1,
                         "No", "Yes") == 0)
                    {
                        return;
                    }
                }

                LastNak.TryRemove(address, out _);
                _controlPanel.AddDevice(_connectionId, address, useCrcCheckBox.Checked,
                    useSecureChannelCheckBox.Checked, key);

                var foundDevice = _settings.Devices.FirstOrDefault(device => device.Address == address);
                if (foundDevice != null)
                {
                    _settings.Devices.Remove(foundDevice);
                }

                _settings.Devices.Add(new DeviceSetting
                {
                    Address = address, Name = nameTextField.Text.ToString(),
                    UseSecureChannel = useSecureChannelCheckBox.Checked,
                    UseCrc = useCrcCheckBox.Checked,
                    SecureChannelKey = key
                });

                Application.RequestStop();
            }

            var addButton = new Button("Add", true);
            addButton.Clicked += AddDeviceButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("Add Device", 60, 13,  cancelButton, addButton);
            dialog.Add(new Label(1, 1, "Name:"),
                nameTextField,
                new Label(1, 3, "Address:"),
                addressTextField,
                useCrcCheckBox,
                useSecureChannelCheckBox,
                new Label(1, 8, "Secure Key:"),
                keyTextField);
            nameTextField.SetFocus();

            Application.Run(dialog);
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
                LastNak.TryRemove(removedDevice.Address, out _);
                _settings.Devices.Remove(removedDevice);
                Application.RequestStop();
            }

            var removeButton = new Button("Remove", true);
            removeButton.Clicked += RemoveDeviceButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("Remove Device", 60, 13,  cancelButton, removeButton);
            dialog.Add(scrollView);
            removeButton.SetFocus();
            
            Application.Run(dialog);
        }

        private static void DiscoverDevice()
        {
            var cancelTokenSrc = new CancellationTokenSource();
            var portNameComboBox = CreatePortNameComboBox(15, 1);
            var pingTimeoutTextField = new TextField(25, 3, 25, "1000");
            var reconnectDelayTextField = new TextField(25, 5, 25, "0");

            void CloseDialog() => Application.RequestStop();

            void OnProgress(DiscoveryResult current)
            {
                string additionalInfo = "";

                switch(current.Status)
                {
                    case DiscoveryStatus.Started:
                        DisplayReceivedReply("Device Discovery Started", String.Empty);
                        // NOTE Unlike other statuses, for this one we are intentionally not dropping down
                        return;
                    case DiscoveryStatus.LookingForDeviceOnConnection:
                        additionalInfo = $"{Environment.NewLine}    Connection baud rate {current.Connection.BaudRate}...";
                        break;
                    case DiscoveryStatus.ConnectionWithDeviceFound:
                        additionalInfo = $"{Environment.NewLine}    Connection baud rate {current.Connection.BaudRate}";
                        break;
                    case DiscoveryStatus.LookingForDeviceAtAddress:
                        additionalInfo = $"{Environment.NewLine}    Address {current.Address}...";
                        break;
                }

                AddLogMessage($"Device Discovery Progress: {current.Status}{additionalInfo}{Environment.NewLine}");
            }

            void CancelDiscover()
            {
                cancelTokenSrc?.Cancel();
                cancelTokenSrc?.Dispose();
                cancelTokenSrc = null;
            }

            void CompleteDiscover()
            {
                DiscoverMenuItem.Title = "_Discover";
                DiscoverMenuItem.Action = DiscoverDevice;
            }

            async void OnClickDiscover() 
            {
                if (string.IsNullOrEmpty(portNameComboBox.Text.ToString()))
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "No port name entered!", "OK");
                    return;
                }

                if (!int.TryParse(pingTimeoutTextField.Text.ToString(), out var pingTimeout))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid reply timeout entered!", "OK");
                    return;
                }

                if (!int.TryParse(reconnectDelayTextField.Text.ToString(), out var reconnectDelay))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid reply timeout entered!", "OK");
                    return;
                }

                CloseDialog();

                try
                {
                    DiscoverMenuItem.Title = "Cancel _Discover";
                    DiscoverMenuItem.Action = CancelDiscover;

                    var result = await _controlPanel.DiscoverDevice(
                        SerialPortOsdpConnection.EnumBaudRates(portNameComboBox.Text.ToString()),
                        new DiscoveryOptions() { 
                            ProgressCallback = OnProgress,
                            ResponseTimeout = TimeSpan.FromMilliseconds(pingTimeout),
                            CancellationToken = cancelTokenSrc.Token,
                            ReconnectDelay = TimeSpan.FromMilliseconds(reconnectDelay),
                        }.WithDefaultTracer(_settings.IsTracing));

                    if (result != null)
                    {
                        AddLogMessage($"Device discovered successfully:{Environment.NewLine}{result}");
                    } 
                    else
                    {
                        AddLogMessage("Device was not found");
                    }
                }
                catch (OperationCanceledException)
                {
                    AddLogMessage("Device discovery cancelled");
                }
                catch (Exception exception)
                {
                    MessageBox.ErrorQuery(40, 10, "Exception in Device Discovery", exception.Message, "OK");
                    AddLogMessage($"Device Discovery Error:{Environment.NewLine}{exception}");
                }
                finally
                {
                    CompleteDiscover();
                }
            }

            var cancelButton = new Button("Cancel");
            var discoverButton = new Button("Discover", true);
            cancelButton.Clicked += CloseDialog;
            discoverButton.Clicked += OnClickDiscover;

            var dialog = new Dialog("Discover Device", 60, 11, cancelButton, discoverButton);
            dialog.Add(new Label(1, 1, "Port:"),
                portNameComboBox,
                new Label(1, 3, "Ping Timeout(ms):"),
                pingTimeoutTextField,
                new Label(1, 5, "Reconnect Delay(ms):"),
                reconnectDelayTextField
            );
            discoverButton.SetFocus();
            Application.Run(dialog);
        }

        private static void SendCommunicationConfiguration()
        {
            var addressTextField = new TextField(20, 1, 20,
                ((_settings.Devices.OrderBy(device => device.Address).LastOrDefault()?.Address ?? 0) + 1).ToString());
            var baudRateTextField = new TextField(20, 3, 20, _settings.SerialConnectionSettings.BaudRate.ToString());

            void SendCommunicationConfigurationButtonClicked()
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
                        if (_settings.SerialConnectionSettings.BaudRate != configuration.BaudRate)
                        {
                            _settings.SerialConnectionSettings.BaudRate = configuration.BaudRate;
                            Application.MainLoop.Invoke(() =>
                            {
                                MessageBox.Query(40, 10, "Info", $"The connection needs to started again with baud rate of {configuration.BaudRate}", "OK");
                            });
                        }
                        
                        _controlPanel.RemoveDevice(_connectionId, address);
                        LastNak.TryRemove(address, out _);
                        
                        var updatedDevice = _settings.Devices.First(device => device.Address == address);
                        updatedDevice.Address = configuration.Address;
                        _controlPanel.AddDevice(_connectionId, updatedDevice.Address, updatedDevice.UseCrc,
                            updatedDevice.UseSecureChannel, updatedDevice.SecureChannelKey);
                    });

                Application.RequestStop();
            }

            var sendButton = new Button("Send", true);
            sendButton.Clicked += SendCommunicationConfigurationButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("Send Communication Configuration Command", 60, 10, cancelButton, sendButton);
            dialog.Add(new Label(1, 1, "Updated Address:"),
                addressTextField,
                new Label(1, 3, "Updated Baud Rate:"),
                baudRateTextField);
            addressTextField.SetFocus();

            Application.Run(dialog);
        }

        private static void SendFileTransferCommand()
        {
            var typeTextField = new TextField(20, 1, 20, "1");
            var messageSizeTextField = new TextField(20, 3, 20, "128");

            void FileTransferButtonClicked()
            {
                if (!byte.TryParse(typeTextField.Text.ToString(), out byte type))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid file transfer type entered!", "OK");
                    return;
                }

                if (!byte.TryParse(messageSizeTextField.Text.ToString(), out byte messageSize))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid message size entered!", "OK");
                    return;
                }

                var openDialog = new OpenDialog("File Transfer", "Select file to transfer");
                if (File.Exists(_settings.LastFileTransferDirectory))
                {
                    var fileInfo = new FileInfo(_settings.LastFileTransferDirectory);
                    openDialog.DirectoryPath = ustring.Make(fileInfo.DirectoryName);
                }
                Application.Run(openDialog);

                string path = openDialog.FilePath.ToString() ?? string.Empty;
                if (!File.Exists(path))
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "No file selected!", "OK");
                    return;
                }

                _settings.LastFileTransferDirectory = path;

                SendCommand("File Transfer", _connectionId, async (connectionId, address) =>
                {
                    var tokenSource = new CancellationTokenSource();
                    var cancelFileTransferButton = new Button("Cancel");
                    cancelFileTransferButton.Clicked += () =>
                    {
                        tokenSource.Cancel();
                        tokenSource.Dispose();
                        Application.RequestStop();
                    };

                    var transferStatusLabel = new Label(new Rect(20, 1, 45, 1), "None");
                    var progressBar = new ProgressBar(new Rect(1, 3, 35, 1));
                    var progressPercentage = new Label(new Rect(40, 3, 10, 1), "0%");
                    
                    Application.MainLoop.Invoke(() =>
                    {
                        var statusDialog = new Dialog("File Transfer Status", 60, 10, cancelFileTransferButton);
                        statusDialog.Add(new Label(1, 1, "Status:"),
                            transferStatusLabel,
                            progressBar,
                            progressPercentage);

                        Application.Run(statusDialog);
                    });

                    var data = await File.ReadAllBytesAsync(path);
                    int fileSize = data.Length;
                    var result = await _controlPanel.FileTransfer(connectionId, address, type, data, messageSize,
                        status =>
                        {
                            Application.MainLoop.Invoke(() =>
                            {
                                transferStatusLabel.Text = status?.Status.ToString();
                                float percentage = (status?.CurrentOffset ?? 0) / (float) fileSize;
                                progressBar.Fraction = percentage;
                                progressPercentage.Text = percentage.ToString("P");

                                if (status?.Status is not (FileTransferStatus.StatusDetail.OkToProceed or FileTransferStatus.StatusDetail.FinishingFileTransfer))
                                {
                                    cancelFileTransferButton.Text = "Close";
                                }
                            });
                        }, tokenSource.Token);

                    return result > 0;
                });

                Application.RequestStop();
            }

            var sendButton = new Button("Send", true);
            sendButton.Clicked += FileTransferButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("File Transfer", 60, 10,cancelButton, sendButton);
            dialog.Add(new Label(1, 1, "Type:"),
                typeTextField,
                new Label(1, 3, "Message Size:"),
                messageSizeTextField);
            typeTextField.SetFocus();

            Application.Run(dialog);
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
                }), _controlPanel.OutputControl, (_, _) => { });

                Application.RequestStop();
            }

            var sendButton = new Button("Send", true);
            sendButton.Clicked += SendOutputControlButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("Send Output Control Command", 60, 10, cancelButton, sendButton);
            dialog.Add(                new Label(1, 1, "Output Number:"),
                outputAddressTextField,
                activateOutputCheckBox);
            outputAddressTextField.SetFocus();
            
            Application.Run(dialog);
        }

        private static void SendReaderLedControlCommand()
        {
            var ledNumberTextField = new TextField(20, 1, 20, "0");
            var colorComboBox = new ComboBox(new Rect(20, 3, 20, 5), Enum.GetNames(typeof(LedColor))) {Text = "Red"};
            
            void SendReaderLedControlButtonClicked()
            {
                if (!byte.TryParse(ledNumberTextField.Text.ToString(), out var ledNumber))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid LED number entered!", "OK");
                    return;
                }

                if (!Enum.TryParse(colorComboBox.Text.ToString(), out LedColor color))
                {

                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid LED color entered!", "OK");
                    return;
                }

                SendCommand("Reader LED Control Command", _connectionId, new ReaderLedControls(new[]
                {
                    new ReaderLedControl(0, ledNumber,
                        TemporaryReaderControlCode.CancelAnyTemporaryAndDisplayPermanent, 1, 0,
                        LedColor.Red, LedColor.Green, 0,
                        PermanentReaderControlCode.SetPermanentState, 1, 0, color, color)
                }), _controlPanel.ReaderLedControl, (_, _) => { });

                Application.RequestStop();
            }

            var sendButton = new Button("Send");
            sendButton.Clicked += SendReaderLedControlButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("Send Reader LED Control Command", 60, 10, cancelButton, sendButton);
            dialog.Add(new Label(1, 1, "LED Number:"),
                ledNumberTextField,
                new Label(1, 3, "Color:"),
                colorComboBox);
            ledNumberTextField.SetFocus();
            
            Application.Run(dialog);
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
                    vendorCode = Convert.FromHexString(vendorCodeTextField.Text.ToString() ?? string.Empty);
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
                    data = Convert.FromHexString(dataTextField.Text.ToString() ?? string.Empty);
                }
                catch
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid data entered!", "OK");
                    return;
                }

                SendCommand("Manufacturer Specific Command", _connectionId,
                    new ManufacturerSpecific(vendorCode.ToArray(), data.ToArray()),
                    _controlPanel.ManufacturerSpecificCommand, (_, _) => { });

                Application.RequestStop();
            }

            var sendButton = new Button("Send", true);
            sendButton.Clicked += SendOutputControlButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("Send Manufacturer Specific Command (Enter Hex Strings)", 60, 10, 
                cancelButton, sendButton);
            dialog.Add(new Label(1, 1, "Vendor Code:"),
                vendorCodeTextField,
                new Label(1, 3, "Data:"),
                dataTextField);
            vendorCodeTextField.SetFocus();
            
            Application.Run(dialog);
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
                    _controlPanel.ReaderBuzzerControl, (_, _) => { });

                Application.RequestStop();
            }

            var sendButton = new Button("Send", true);
            sendButton.Clicked += SendReaderBuzzerControlButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("Send Reader Buzzer Control Command", 60, 10, cancelButton, sendButton);
            dialog.Add(new Label(1, 1, "Reader Number:"),
                readerAddressTextField,
                new Label(1, 3, "Repeat Times:"),
                repeatTimesTextField);
            readerAddressTextField.SetFocus();
            
            Application.Run(dialog);
        }

        private static void SendBiometricReadCommand()
        {
            var readerAddressTextField = new TextField(20, 1, 20, "0");
            var typeTextField = new TextField(20, 3, 20, "0");
            var formatTextField = new TextField(20, 5, 20, "2");
            var qualityTextField = new TextField(20, 7, 20, "50");
            
            void SendBiometricReadButtonClicked()
            {
                if (!byte.TryParse(readerAddressTextField.Text.ToString(), out byte readerNumber))
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid reader number entered!", "OK");
                    return;
                }
                if (!byte.TryParse(typeTextField.Text.ToString(), out byte type))
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid Bio type number entered!", "OK");
                    return;
                }
                if (!byte.TryParse(formatTextField.Text.ToString(), out byte format))
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid format number entered!", "OK");
                    return;
                }
                if (!byte.TryParse(qualityTextField.Text.ToString(), out byte quality))
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid quality number entered!", "OK");
                    return;
                }

                SendCommand("Biometric Read Command", _connectionId, 
                    new BiometricReadData(readerNumber, (BiometricType)type, (BiometricFormat)format, quality), TimeSpan.FromSeconds(30),
                    // ReSharper disable once AsyncVoidLambda
                    _controlPanel.ScanAndSendBiometricData, async (_, result) =>
                    {
                        DisplayReceivedReply($"Received Bio Read", result.ToString());
                        
                        if (result.TemplateData.Length > 0)
                        {
                            await File.WriteAllBytesAsync("BioReadTemplate", result.TemplateData);
                        }
                    });

                Application.RequestStop();
            }

            var sendButton = new Button("Send", true);
            sendButton.Clicked += SendBiometricReadButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("Biometric Read Command", 60, 12, cancelButton, sendButton);
            dialog.Add(new Label(1, 1, "Reader Number:"),
                readerAddressTextField);
            dialog.Add(new Label(1, 3, "Bio Type:"),
                typeTextField);
            dialog.Add(new Label(1, 5, "Bio Format:"),
                formatTextField);
            dialog.Add(new Label(1, 7, "Quality:"),
                qualityTextField);
            readerAddressTextField.SetFocus();
            
            Application.Run(dialog);
        }

        private static void SendBiometricMatchCommand()
        {
            var readerAddressTextField = new TextField(20, 1, 20, "0");
            var typeTextField = new TextField(20, 3, 20, "0");
            var formatTextField = new TextField(20, 5, 20, "2");
            var qualityThresholdTextField = new TextField(20, 7, 20, "50");

            void SendBiometricMatchButtonClicked()
            {
                if (!byte.TryParse(readerAddressTextField.Text.ToString(), out byte readerNumber))
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid reader number entered!", "OK");
                    return;
                }
                if (!byte.TryParse(typeTextField.Text.ToString(), out byte type))
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid Bio type number entered!", "OK");
                    return;
                }
                if (!byte.TryParse(formatTextField.Text.ToString(), out byte format))
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid format number entered!", "OK");
                    return;
                }
                if (!byte.TryParse(qualityThresholdTextField.Text.ToString(), out byte qualityThreshold))
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid quality threshold number entered!", "OK");
                    return;
                }
                
                var openDialog = new OpenDialog("Biometric Match", "Select a template to match");
                openDialog.DirectoryPath = ustring.Make(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                Application.Run(openDialog);

                string path = openDialog.FilePath.ToString() ?? string.Empty;
                if (!File.Exists(path))
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "No file selected!", "OK");
                    return;
                }

                SendCommand("Biometric Match Command", _connectionId, 
                    new BiometricTemplateData(readerNumber, (BiometricType)type, (BiometricFormat)format,
                        qualityThreshold, File.ReadAllBytes(path)), TimeSpan.FromSeconds(30),
                    _controlPanel.ScanAndMatchBiometricTemplate, (_, _) => { });

                Application.RequestStop();
            }

            var sendButton = new Button("Send", true);
            sendButton.Clicked += SendBiometricMatchButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("Biometric Match Command", 60, 12, cancelButton, sendButton);
            dialog.Add(new Label(1, 1, "Reader Number:"),
                readerAddressTextField);
            dialog.Add(new Label(1, 3, "Bio Type:"),
                typeTextField);
            dialog.Add(new Label(1, 5, "Bio Format:"),
                formatTextField);
            dialog.Add(new Label(1, 7, "Quality Threshold:"),
                qualityThresholdTextField);
            readerAddressTextField.SetFocus();
            
            Application.Run(dialog);
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
                    _controlPanel.ReaderTextOutput, (_, _) => { });

                Application.RequestStop();
            }

            var sendButton = new Button("Send", true);
            sendButton.Clicked += SendReaderTextOutputButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("Reader Text Output Command", 60, 10, cancelButton, sendButton);
            dialog.Add(new Label(1, 1, "Reader Number:"),
                readerAddressTextField,
                new Label(1, 3, "Text Output:"),
                textOutputTextField);
            readerAddressTextField.SetFocus();
            
            Application.Run(dialog);
        }

        private static void SendEncryptionKeySetCommand()
        {
            var keyTextField = new TextField(20, 1, 32, string.Empty);

            void SendButtonClicked()
            {
                if (keyTextField.Text == null || keyTextField.Text.Length != 32)
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid key length entered!", "OK");
                    return;
                }

                byte[] key;
                try
                {
                    key = Convert.FromHexString(keyTextField.Text.ToString()!);
                }
                catch
                {
                    MessageBox.ErrorQuery(40, 10, "Error", "Invalid hex characters!", "OK");
                    return;
                }
                
                MessageBox.ErrorQuery(40, 10, "Warning", "The new key will be required to access the device in the future. Saving the updated configuration will store the key in clear text.", "OK");
                
                SendCommand("Encryption Key Configuration", _connectionId,
                    new EncryptionKeyConfiguration(KeyType.SecureChannelBaseKey, key),
                    _controlPanel.EncryptionKeySet,
                    (address, result) =>
                    {
                        if (!result)
                        {
                            return;
                        }

                        LastNak.TryRemove(address, out _);

                        var updatedDevice = _settings.Devices.First(device => device.Address == address);
                        updatedDevice.UseSecureChannel = true;
                        updatedDevice.SecureChannelKey = key;

                        _controlPanel.AddDevice(_connectionId, updatedDevice.Address, updatedDevice.UseCrc,
                            updatedDevice.UseSecureChannel, updatedDevice.SecureChannelKey);
                    }, true);

                Application.RequestStop();
            }

            var sendButton = new Button("Send", true);
            sendButton.Clicked += SendButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog("Encryption Key Set Command (Enter Hex String)", 60, 8, cancelButton, sendButton);
            dialog.Add(new Label(1, 1, "Key:"),
                keyTextField);
            keyTextField.SetFocus();
                
            Application.Run(dialog);
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

            var sendButton = new Button("Send", true);
            sendButton.Clicked += SendCommandButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog(title, 60, 13, cancelButton, sendButton);
            dialog.Add(deviceSelectionView);
            sendButton.SetFocus();
            
            Application.Run(dialog);
        }

        private static void SendCommand<T, TU>(string title, Guid connectionId, TU commandData,
            Func<Guid, byte, TU, Task<T>> sendCommandFunction, Action<byte, T> handleResult, bool requireSecurity = false)
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

                if (requireSecurity && !selectedDevice.UseSecureChannel)
                {
                    MessageBox.ErrorQuery(60, 10, "Warning", "Requires secure channel to process this command.", "OK");
                    return;
                }

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

            var sendButton = new Button("Send", true);
            sendButton.Clicked += SendCommandButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog(title, 60, 13, cancelButton, sendButton);
            dialog.Add(deviceSelectionView);
            sendButton.SetFocus();
            
            Application.Run(dialog);
        }

        private static void SendCommand<T1, T2, T3>(string title, Guid connectionId, T2 commandData, T3 timeOut,
            Func<Guid, byte, T2, T3, CancellationToken, Task<T1>> sendCommandFunction, Action<byte, T1> handleResult, bool requireSecurity = false)
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

                if (requireSecurity && !selectedDevice.UseSecureChannel)
                {
                    MessageBox.ErrorQuery(60, 10, "Warning", "Requires secure channel to process this command.", "OK");
                    return;
                }

                Task.Run(async () =>
                {
                    try
                    {
                        var result = await sendCommandFunction(connectionId, address, commandData, timeOut, CancellationToken.None);
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

            var sendButton = new Button("Send", true);
            sendButton.Clicked += SendCommandButtonClicked;
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog(title, 60, 13, cancelButton, sendButton);
            dialog.Add(deviceSelectionView);
            sendButton.SetFocus();
            
            Application.Run(dialog);
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
            cancelButton.Clicked += () => Application.RequestStop();

            var dialog = new Dialog(title, 60, 13, cancelButton, sendButton);
            dialog.Add(deviceSelectionView);
            sendButton.SetFocus();
            
            Application.Run(dialog);
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
                orderedDevices.Select(device => ustring.Make($"{device.Address} : {device.Name}")).ToArray())
            {
                SelectedItem = 0
            };
            scrollView.Add(deviceRadioGroup);
            return scrollView;
        }
    }
}

