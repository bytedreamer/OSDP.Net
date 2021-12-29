using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace OSDP.Net.Connections
{
    /// <summary>Connect using a serial port.</summary>
    public class SerialPortOsdpConnection : IOsdpConnection
    {
        private readonly SerialPort _serialPort = new();

        /// <summary>Initializes a new instance of the <see cref="T:OSDP.Net.Connections.SerialPortOsdpConnection" /> class.</summary>
        /// <param name="portName">Name of the port.</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <exception cref="T:System.ArgumentNullException">portName</exception>
        public SerialPortOsdpConnection(string portName, int baudRate)
        {
            _serialPort.PortName = portName ?? throw new ArgumentNullException(nameof(portName));
            _serialPort.BaudRate = baudRate;
        }

        /// <inheritdoc />
        public int BaudRate => _serialPort.BaudRate;

        /// <inheritdoc />
        public bool IsOpen => _serialPort.IsOpen;

        /// <inheritdoc />
        public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <inheritdoc />
        public void Open()
        {
            _serialPort.Open();
        }

        /// <inheritdoc />
        public void Close()
        {
            _serialPort.Close();
        }

        /// <inheritdoc />
        public async Task WriteAsync(byte[] buffer)
        {
            await _serialPort.BaseStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
        {
            return await _serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
        }
    }
}
