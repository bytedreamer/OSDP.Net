using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading.Tasks;

namespace OSDP.Net
{
    public class SerialPortOsdpConnection : IOsdpConnection
    {
        private readonly SerialPort _serialPort = new SerialPort();
        private readonly ConcurrentQueue<byte> _queue = new ConcurrentQueue<byte>();

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
            return await _serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length);
        }
    }
}
