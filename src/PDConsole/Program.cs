using System;
using System.IO;
using System.Text.Json;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Logging;
using PDConsole.Configuration;
using Terminal.Gui;

namespace PDConsole
{
    /// <summary>
    /// Main program class
    /// </summary>
    class Program
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));
        private static PDConsoleController _controller;
        private static PDConsoleView _view;

        static void Main()
        {
            ConfigureLogging();
            
            try
            {
                // Load settings
                var settings = LoadSettings();
                
                // Create a logger factory
                var loggerFactory = new LoggerFactory();
                
                // Create controller (ViewModel)
                _controller = new PDConsoleController(settings, loggerFactory);
                
                // Initialize Terminal.Gui
                Application.Init();
                
                // Create view
                _view = new PDConsoleView(_controller);
                
                // Create and add a main window
                var mainWindow = _view.CreateMainWindow();
                Application.Top.Add(mainWindow);
                
                // Run the application
                Application.Run();
            }
            catch (Exception ex)
            {
                Logger.Fatal("Fatal error occurred", ex);
                Console.WriteLine($"Fatal error: {ex.Message}");
            }
            finally
            {
                Cleanup();
            }
        }

        private static void ConfigureLogging()
        {
            var logRepository = LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        }

        private static Settings LoadSettings()
        {
            const string settingsFile = "appsettings.json";
            
            if (File.Exists(settingsFile))
            {
                try
                {
                    var json = File.ReadAllText(settingsFile);
                    return JsonSerializer.Deserialize<Settings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error("Error loading settings", ex);
                    return new Settings();
                }
            }
            else
            {
                var defaultSettings = new Settings();
                SaveSettings(defaultSettings);
                return defaultSettings;
            }
        }

        private static void SaveSettings(Settings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
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

        private static void Cleanup()
        {
            try
            {
                _controller?.Dispose();
                Application.Shutdown();
            }
            catch (Exception ex)
            {
                Logger.Error("Error during cleanup", ex);
            }
        }
    }
}