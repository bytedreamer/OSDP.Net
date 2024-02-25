using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OSDP.Net.Connections
{
    /// <summary>Connect using a serial port.</summary>
    public class SerialPortOsdpConnection : OsdpConnection
    {
        private readonly string _portName;
        private SerialPort _serialPort;

        /// <summary>Initializes a new instance of the <see cref="T:OSDP.Net.Connections.SerialPortOsdpConnection" /> class.</summary>
        /// <param name="portName">Name of the port.</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <exception cref="T:System.ArgumentNullException">portName</exception>
        public SerialPortOsdpConnection(string portName, int baudRate) : base(baudRate)
        {
            _portName = portName ?? throw new ArgumentNullException(nameof(portName));
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
            rates ??= [9600, 19200, 38400, 57600, 115200, 230400];
            return rates.AsEnumerable().Select((rate) => new SerialPortOsdpConnection(portName, rate));
        }

        /// <inheritdoc />
        public override Task Open()
        {
            if (_serialPort == null)
            {
                _serialPort = new(_portName, BaudRate);
                _serialPort.Open();
                IsOpen = true;
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task Close()
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
            _serialPort = null;
            IsOpen = false;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override async Task WriteAsync(byte[] buffer)
        {
            // Found an issue where many timeouts would fill up the receive buffer.
            // When writing to the port, there should be nothing in the buffers.
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
            
            await _serialPort.BaseStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
        {
            var task = _serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length, token);

            if (await Task.WhenAny(task, Task.Delay(-1, token)) == task)
            {
                return await task.ConfigureAwait(false);
            }

            throw new TimeoutException();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _portName;
        }
    }

    /// <summary>
    /// Implements OSDP server side which communicates via a serial port
    /// </summary>
    /// <remarks>
    /// Whereas TCP/IP server creates a new connection whenever listener detects a new client,
    /// serial server operates differently. It will instantaneously connect to the serial port
    /// and open its side of comms without waiting for anyone/anything to connect to the other
    /// side of the serial cable.
    /// </remarks>
    public class SerialPortOsdpServer : OsdpServer
    {
        private readonly string _portName;
        private readonly int _baudRate;

        /// <summary>
        /// Creates a new instance of SerialPortOsdpServer
        /// </summary>
        /// <param name="portName">Name of the serial port</param>
        /// <param name="baudRate">Baud rate at which to communicate</param>
        /// <param name="loggerFactory">Optional logger factory</param>
        public SerialPortOsdpServer(
            string portName, int baudRate, ILoggerFactory loggerFactory = null) : base(loggerFactory)
        {
            _portName = portName;
            _baudRate = baudRate;
        }

        /// <inheritdoc/>
        public override async Task Start(Func<IOsdpConnection, Task> newConnectionHandler)
        {
            IsRunning = true;

            Logger?.LogInformation("Opening {port} @ {baud} serial port...", _portName, _baudRate);

            await OpenSerialPort(newConnectionHandler);
        }

        /// <inheritdoc/>
        public override Task Stop() => base.Stop();

        private async Task OpenSerialPort(Func<IOsdpConnection, Task> newConnectionHandler)
        {
            var connection = new SerialPortOsdpConnection(_portName, _baudRate);
            await connection.Open();
            var task = Task.Run(async () =>
            {
                await newConnectionHandler(connection);
                if (IsRunning)
                {
                    await Task.Delay(1);
                    await OpenSerialPort(newConnectionHandler);
                }
            });
            RegisterConnection(connection, task);
        }
    }
}
