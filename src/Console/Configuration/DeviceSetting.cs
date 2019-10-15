namespace Console.Configuration
{
    public class DeviceSetting
    {
        public string Name { get; set; }
        public byte Address { get; set; }
        public bool UseSecureChannel { get; set; } = false;
        public bool UseCrc { get; set; } = true;
    }
}