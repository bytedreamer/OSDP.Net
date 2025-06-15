using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Logging;
using OSDP.Net;
using OSDP.Net.Connections;
using PDConsole.Configuration;
using Terminal.Gui;

namespace PDConsole
{
    class Program
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));
        private static Settings _settings;
        private static PDDevice _device;
        private static IOsdpConnectionListener _connectionListener;
        private static CancellationTokenSource _cancellationTokenSource;
        
        private static ListView _commandHistoryView;
        private static Label _statusLabel;
        private static Label _connectionLabel;
        private static TextField _cardDataField;
        private static TextField _keypadField;
        private static readonly List<CommandEvent> CommandHistoryItems = [];
        
        static void Main()
        {
            ConfigureLogging();
            LoadSettings();
            
            Application.Init();
            
            try
            {
                var top = Application.Top;
                
                // Create the main window
                var win = new Window("OSDP.Net PD Console")
                {
                    X = 0,
                    Y = 1,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };
                top.Add(win);
                
                // Create a menu bar
                var menu = new MenuBar([
                    new MenuBarItem("_File", [
                        new MenuItem("_Settings", "", ShowSettingsDialog),
                        new MenuItem("_Quit", "", RequestStop)
                    ]),
                    new MenuBarItem("_Device", [
                        new MenuItem("_Start", "", StartDevice),
                        new MenuItem("S_top", "", StopDevice),
                        new MenuItem("_Clear History", "", ClearHistory)
                    ])
                ]);
                top.Add(menu);
                
                // Device status frame
                var statusFrame = new FrameView("Device Status")
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = 5
                };
                win.Add(statusFrame);
                
                _connectionLabel = new Label("Connection: Not Started")
                {
                    X = 1,
                    Y = 0
                };
                statusFrame.Add(_connectionLabel);
                
                _statusLabel = new Label($"Address: {_settings.Device.Address} | Security: {(_settings.Security.RequireSecureChannel ? "Enabled" : "Disabled")}")
                {
                    X = 1,
                    Y = 1
                };
                statusFrame.Add(_statusLabel);
                
                // Simulation controls frame
                var simulationFrame = new FrameView("Simulation Controls")
                {
                    X = 0,
                    Y = Pos.Bottom(statusFrame),
                    Width = Dim.Fill(),
                    Height = 6
                };
                win.Add(simulationFrame);
                
                var cardDataLabel = new Label("Card Data (Hex):")
                {
                    X = 1,
                    Y = 1
                };
                simulationFrame.Add(cardDataLabel);
                
                _cardDataField = new TextField("0123456789ABCDEF")
                {
                    X = Pos.Right(cardDataLabel) + 1,
                    Y = 1,
                    Width = 30
                };
                simulationFrame.Add(_cardDataField);
                
                var sendCardButton = new Button("Send Card")
                {
                    X = Pos.Right(_cardDataField) + 1,
                    Y = 1
                };
                sendCardButton.Clicked += () =>
                {
                    if (_device != null && !string.IsNullOrEmpty(_cardDataField.Text.ToString()))
                    {
                        _device.SendSimulatedCardRead(_cardDataField.Text.ToString());
                    }
                };
                simulationFrame.Add(sendCardButton);
                
                var keypadLabel = new Label("Keypad Data:")
                {
                    X = 1,
                    Y = 3
                };
                simulationFrame.Add(keypadLabel);
                
                _keypadField = new TextField("1234")
                {
                    X = Pos.Right(keypadLabel) + 1,
                    Y = 3,
                    Width = 20
                };
                simulationFrame.Add(_keypadField);
                
                var sendKeypadButton = new Button("Send Keypad")
                {
                    X = Pos.Right(_keypadField) + 1,
                    Y = 3
                };
                sendKeypadButton.Clicked += () =>
                {
                    if (_device != null && !string.IsNullOrEmpty(_keypadField.Text.ToString()))
                    {
                        _device.SimulateKeypadEntry(_keypadField.Text.ToString());
                    }
                };
                simulationFrame.Add(sendKeypadButton);
                
                // Command history frame
                var historyFrame = new FrameView("Command History")
                {
                    X = 0,
                    Y = Pos.Bottom(simulationFrame),
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };
                win.Add(historyFrame);
                
                _commandHistoryView = new ListView()
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };
                UpdateCommandHistoryView();
                _commandHistoryView.OpenSelectedItem += ShowCommandDetails;
                historyFrame.Add(_commandHistoryView);
                
                Application.Run();
            }
            finally
            {
                StopDevice();
                Application.Shutdown();
            }
        }
        
        private static void ConfigureLogging()
        {
            var logRepository = LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        }
        
        private static void LoadSettings()
        {
            const string settingsFile = "appsettings.json";
            
            if (File.Exists(settingsFile))
            {
                try
                {
                    var json = File.ReadAllText(settingsFile);
                    _settings = JsonSerializer.Deserialize<Settings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error("Error loading settings", ex);
                    _settings = new Settings();
                }
            }
            else
            {
                _settings = new Settings();
                SaveSettings();
            }
        }
        
        private static void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText("appsettings.json", json);
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving settings", ex);
            }
        }
        
        private static void StartDevice()
        {
            if (_device != null || _connectionListener != null)
            {
                MessageBox.ErrorQuery("Error", "Device is already running!", "OK");
                return;
            }
            
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                
                // Create a simple logger factory
                var loggerFactory = new LoggerFactory();
                
                // Create device configuration
                var deviceConfig = new DeviceConfiguration
                {
                    Address = _settings.Device.Address,
                    RequireSecurity = _settings.Security.RequireSecureChannel,
                    SecurityKey = _settings.Security.SecureChannelKey
                };
                
                // Create the device
                _device = new PDDevice(deviceConfig, _settings.Device, loggerFactory);
                _device.CommandReceived += OnCommandReceived;
                
                // Create a connection listener based on type
                switch (_settings.Connection.Type)
                {
                    case ConnectionType.Serial:
                        _connectionListener = new SerialPortConnectionListener(
                            _settings.Connection.SerialPortName,
                            _settings.Connection.SerialBaudRate);
                        break;
                        
                    case ConnectionType.TcpServer:
                        _connectionListener = new TcpConnectionListener(
                            _settings.Connection.TcpServerPort,
                            9600, // Default baud rate for TCP (not really used)
                            loggerFactory);
                        break;
                        
                    default:
                        throw new NotSupportedException($"Connection type {_settings.Connection.Type} not supported");
                }
                
                // Start listening
                _device.StartListening(_connectionListener);
                
                _connectionLabel.Text = $"Connection: Listening on {GetConnectionString()}";
            }
            catch (Exception ex)
            {
                Logger.Error("Error starting device", ex);
                MessageBox.ErrorQuery("Error", $"Failed to start device: {ex.Message}", "OK");
                StopDevice();
            }
        }
        
        private static void StopDevice()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _connectionListener?.Dispose();
                
                if (_device != null)
                {
                    _device.CommandReceived -= OnCommandReceived;
                    _ = _device.StopListening();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error stopping device", ex);
            }
            finally
            {
                _device = null;
                _connectionListener = null;
                _cancellationTokenSource = null;
                
                if (_connectionLabel != null)
                {
                    _connectionLabel.Text = "Connection: Not Started";
                }
            }
        }
        
        private static void OnCommandReceived(object sender, CommandEvent e)
        {
            Application.MainLoop.Invoke(() =>
            {
                CommandHistoryItems.Add(e);
                
                // Keep only the last 100 items in UI
                if (CommandHistoryItems.Count > 100)
                {
                    CommandHistoryItems.RemoveAt(0);
                }
                
                UpdateCommandHistoryView();
                _commandHistoryView.SelectedItem = CommandHistoryItems.Count - 1;
                _commandHistoryView.EnsureSelectedItemVisible();
            });
        }
        
        private static void ClearHistory()
        {
            CommandHistoryItems.Clear();
            UpdateCommandHistoryView();
        }
        
        private static void UpdateCommandHistoryView()
        {
            var displayItems = CommandHistoryItems
                .Select(e => $"{e.Timestamp:T} - {e.Description}")
                .ToList();
            _commandHistoryView.SetSource(displayItems);
        }
        
        private static void ShowCommandDetails(ListViewItemEventArgs e)
        {
            if (e.Item >= 0 && e.Item < CommandHistoryItems.Count)
            {
                var commandEvent = CommandHistoryItems[e.Item];
                var details = string.IsNullOrEmpty(commandEvent.Details) 
                    ? "No additional details available." 
                    : commandEvent.Details;
                
                // Create a custom dialog for left-justified text
                var dialog = new Dialog("Command Details")
                {
                    Width = Dim.Percent(80),
                    Height = Dim.Percent(70)
                };
                
                // Create a TextView for the details content
                var textView = new TextView()
                {
                    X = 1,
                    Y = 1,
                    Width = Dim.Fill(1),
                    Height = Dim.Fill(2),
                    ReadOnly = true,
                    Text = $" Command: {commandEvent.Description}\n" +
                              $"    Time: {commandEvent.Timestamp:s} {commandEvent.Timestamp:t}\n" +
                              $"\n" +
                              $" {new string('─', 60)}\n" +
                              $"\n" +
                              string.Join("\n", details.Split('\n').Select(line => $" {line}"))
                };
                
                dialog.Add(textView);
                
                // Add OK button
                var okButton = new Button("OK")
                {
                    X = Pos.Center(),
                    Y = Pos.Bottom(dialog) - 3,
                    IsDefault = true
                };
                okButton.Clicked += () => Application.RequestStop(dialog);
                
                dialog.Add(okButton);
                
                // Make the dialog focusable and handle escape key
                dialog.AddButton(okButton);
                
                Application.Run(dialog);
            }
        }
        
        private static void ShowSettingsDialog()
        {
            MessageBox.Query("Settings", "Settings dialog not yet implemented.\nEdit appsettings.json manually.", "OK");
        }
        
        private static void RequestStop()
        {
            StopDevice();
            Application.RequestStop();
        }
        
        private static string GetConnectionString()
        {
            return _settings.Connection.Type switch
            {
                ConnectionType.Serial => $"{_settings.Connection.SerialPortName} @ {_settings.Connection.SerialBaudRate}",
                ConnectionType.TcpServer => $"{_settings.Connection.TcpServerAddress}:{_settings.Connection.TcpServerPort}",
                _ => "Unknown"
            };
        }
    }
}