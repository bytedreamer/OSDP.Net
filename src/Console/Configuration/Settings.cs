using System.Collections.Generic;

namespace Console.Configuration
{
    public class Settings
    {
        public ConnectionSettings ConnectionSettings { get; set; } = new ConnectionSettings();

        public List<DeviceSetting> Devices { get; set; } = new List<DeviceSetting>();
    }
}