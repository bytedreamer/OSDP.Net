namespace Console.Configuration
{
    public class ConnectionSettings
    {
        public string PortName { get; set; } = "/dev/tty.SLAB_USBtoUART";
        public int BaudRate { get; set; } = 9600;
    }
}