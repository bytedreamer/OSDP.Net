using System;
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
        private readonly string _portName;
        private readonly int _baudRate;
        private SerialPort _serialPort;

        /// <summary>Initializes a new instance of the <see cref="T:OSDP.Net.Connections.SerialPortOsdpConnection" /> class.</summary>
        /// <param name="portName">Name of the port.</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <exception cref="T:System.ArgumentNullException">portName</exception>
        public SerialPortOsdpConnection(string portName, int baudRate)
        {
            _portName = portName ?? throw new ArgumentNullException(nameof(portName));
            _baudRate = baudRate;
        }

        /// <summary>
        /// A helper method that returns a lazily instantiated set of SerialPortOsdpConnection
        /// instances, each one configured for a different baud rate. The primary use case for this
        /// method is in conjunction with <see cref="ControlPanel.DiscoverDevice(IEnumerable{IOsdpConnection}, PanelCommands.DeviceDiscover.DiscoveryOptions)"/>
        /// which expects a set of connections to test for a device.
        /// </summary>
        /// <param name="portName">Name of the port</param>
        /// <param name="rates">
        /// Optional parameter identifying a set of baud rates to enumerate over. If not specified,
        /// the list from OSDP spec (9600, 19200, 38400, 57600, 115200, 23040) will be used by default
        /// </param>
        /// <returns>An enumerable that will lazily generate SerialPortOsdpConnection instances for a 
        /// given set of baud rates (see description of "rates" parameter)</returns>
        public static IEnumerable<SerialPortOsdpConnection> EnumBaudRates(string portName, int[] rates=null)
        {
            rates ??= new[] { 9600, 19200, 38400, 57600, 115200, 230400 };
            return rates.AsEnumerable().Select((rate) => new SerialPortOsdpConnection(portName, rate));
        }

        /// <inheritdoc />
        public int BaudRate => _baudRate;

        /// <inheritdoc />
        public bool IsOpen => _serialPort != null && _serialPort.IsOpen;

        /// <inheritdoc />
        public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <inheritdoc />
        public void Open()
        {
            if (_serialPort != null) return;
            _serialPort = new(_portName, _baudRate);
            _serialPort.Open();
        }

        /// <inheritdoc />
        public void Close()
        {
            if (_serialPort == null) return;
            _serialPort.Close();
            _serialPort.Dispose();
            _serialPort = null;
        }

        /// <inheritdoc />
        public async Task WriteAsync(byte[] buffer)
        {
            // Found an issue where many timeouts would fill up the receive buffer.
            // When writing to the port, there should be nothing in the buffers.
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
            
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
