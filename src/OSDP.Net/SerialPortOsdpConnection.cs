using System.IO.Ports;
using System.Threading.Tasks;

namespace OSDP.Net
{
    public class SerialPortOsdpConnection : IOsdpConnection
    {
        private readonly SerialPort _serialPort = new SerialPort();

        public bool IsOpen => _serialPort.IsOpen;

        public void Open()
        {
            _serialPort.PortName = "/dev/tty.SLAB_USBtoUART";
            _serialPort.ReadTimeout = 200;

            _serialPort.Open();
        }

        public void Close()
        {
            _serialPort.Close();
        }

        public async Task Write(byte[] buffer)
        {
            await _serialPort.BaseStream.WriteAsync(buffer, 0, buffer.Length);
        }

        public async Task<int> Read(byte[] buffer)
        {
            var readTask =  _serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length);
            var delayTask = Task.Delay(_serialPort.ReadTimeout);
            var task = await Task.WhenAny(readTask, delayTask);
            if (task == readTask)
            {
                return await readTask;
            }

            return 0;
        }
    }
}
