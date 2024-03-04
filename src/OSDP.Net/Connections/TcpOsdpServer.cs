using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OSDP.Net.Connections;

/// <summary>
/// Implements TCP/IP OSDP server which listens for incoming connections
/// </summary>
public class TcpOsdpServer : OsdpServer
{
    private readonly TcpListener _listener;
    private readonly int _baudRate;

    /// <summary>
    /// Creates a new instance of TcpOsdpServer
    /// </summary>
    /// <param name="portNumber">Port to listen on</param>
    /// <param name="baudRate">Baud rate at which comms are expected to take place</param>
    /// <param name="loggerFactory">Optional logger factory</param>
    public TcpOsdpServer(
        int portNumber, int baudRate, ILoggerFactory loggerFactory = null) : base(loggerFactory)
    {
        _listener = TcpListener.Create(portNumber);
        _baudRate = baudRate;
    }

    /// <inheritdoc/>
    public override Task Start(Func<IOsdpConnection, Task> newConnectionHandler)
    {
        if (IsRunning) return Task.CompletedTask;

        IsRunning = true;
        _listener.Start();

        Logger?.LogInformation("Listening on {Endpoint} for incoming connections...", _listener.LocalEndpoint.ToString());

        Task.Run(async () =>
        {
            while (IsRunning)
            {
                var client = await _listener.AcceptTcpClientAsync();

                var connection = new TcpServerOsdpConnection2(client, _baudRate, LoggerFactory);
                var task = newConnectionHandler(connection);
                RegisterConnection(connection, task);
            }
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task Stop()
    {
        IsRunning = false;
        _listener.Stop();
        return base.Stop();
    }
}