using System.Collections.Generic;

namespace Console.Configuration
{
    public class Settings
    {
        public SerialConnectionSettings SerialConnectionSettings { get; set; } = new SerialConnectionSettings();

        public TcpServerConnectionSettings TcpServerConnectionSettings { get; set; } = new TcpServerConnectionSettings();

        public TcpClientConnectionSettings TcpClientConnectionSettings { get; set; } = new TcpClientConnectionSettings();

        public List<DeviceSetting> Devices { get; set; } = new List<DeviceSetting>();
    }
}