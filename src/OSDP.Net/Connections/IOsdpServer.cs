using System;
using System.Threading.Tasks;

namespace OSDP.Net.Connections;

/// <summary>
/// Defines a server side of OSDP connection which is intended to listen for
/// incoming connections as they are established
/// </summary>

public interface IOsdpServer : IDisposable
{
    /// <summary>
    /// Baud rate for the current connection
    /// </summary>
    int BaudRate { get; }

    /// <summary>
    /// Starts listening for incoming connections
    /// </summary>
    /// <param name="newConnectionHandler">Callback to be invoked whenever a new connection is accepted</param>
    Task Start(Func<IOsdpConnection, Task> newConnectionHandler);

    /// <summary>
    /// Stops the server, which stops the listener and terminates
    /// any presently open connections to the server
    /// </summary>
    /// <returns></returns>
    Task Stop();

    /// <summary>
    /// Indicates whether or not the server is running
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// The number of active connections being tracked by the server
    /// </summary>
    int ConnectionCount { get; }
}