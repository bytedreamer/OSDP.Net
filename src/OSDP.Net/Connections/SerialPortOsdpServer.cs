using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OSDP.Net.Connections;

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

        Logger?.LogInformation("Opening {Port} @ {Baud} serial port...", _portName, _baudRate);

        await OpenSerialPort(newConnectionHandler);
    }

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