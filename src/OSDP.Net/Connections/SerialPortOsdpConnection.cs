using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
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

        /// <summary>
        /// Enumerates all valid baud rates for a given COM port
        /// </summary>
        /// <param name="portName">Name of the port</param>
        /// <returns>An enumerable that will lazily generate SerialPortOsdpConnection instances for all
        /// valid baud rates in the increasing order</returns>
        public static IEnumerable<SerialPortOsdpConnection> EnumBaudRates(string portName)
        {
            // TODO: Allow the caller to specify a different reply timeout

            var rates = new[] { 9600, 19200, 38400, 57600, 115200, 230400 };
            return rates.AsEnumerable().Select((rate) => new SerialPortOsdpConnection(portName, rate));
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
            var task = _serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length, token);

            if (await Task.WhenAny(task, Task.Delay(-1, token)) == task)
            {
                return await task.ConfigureAwait(false);
            }

            throw new TimeoutException();
        }
    }
}
