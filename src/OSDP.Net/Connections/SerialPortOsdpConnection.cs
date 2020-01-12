using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace OSDP.Net.Connections
{
    public class SerialPortOsdpConnection : IOsdpConnection
    {
        private readonly SerialPort _serialPort = new SerialPort();

        public SerialPortOsdpConnection(string portName, int baudRate)
        {
            _serialPort.PortName = portName ?? throw new ArgumentNullException(nameof(portName));
            _serialPort.BaudRate = baudRate;
        }

        public int BaudRate => _serialPort.BaudRate;

        public bool IsOpen => _serialPort.IsOpen;

        public void Open()
        {
            _serialPort.Open();
        }

        public void Close()
        {
            _serialPort.Close();
        }

        public async Task WriteAsync(byte[] buffer)
        {
            await _serialPort.BaseStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
        }

        public async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
        {
            return await _serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
        }
    }
}
