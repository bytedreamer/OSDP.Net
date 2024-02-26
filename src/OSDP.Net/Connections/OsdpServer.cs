using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OSDP.Net.Connections;

/// <summary>
/// Base class for an OSDP server that listens for incoming connections
/// </summary>
public abstract class OsdpServer : IOsdpServer
{
    private bool _disposedValue;
    private readonly ConcurrentDictionary<Task, OsdpConnection> _connections = new();

    /// <summary>
    /// Creates a new instance of OsdpServer
    /// </summary>
    /// <param name="loggerFactory">Optional logger factory</param>
    protected OsdpServer(ILoggerFactory loggerFactory = null)
    {
        LoggerFactory = loggerFactory;
        Logger = loggerFactory?.CreateLogger<OsdpServer>();
    }

    /// <inheritdoc/>
    public bool IsRunning { get; protected set; }

    /// <inheritdoc/>
    public int ConnectionCount => _connections.Count;

    /// <summary>
    /// Logger factory if one was specified at instantitation
    /// </summary>
    protected ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// Logger instance used by the server if a factory was specified at instantiation
    /// </summary>
    protected ILogger Logger { get; }

    /// <inheritdoc/>
    public abstract Task Start(Func<IOsdpConnection, Task> newConnectionHandler);

    /// <inheritdoc/>
    public virtual async Task Stop()
    {
        IsRunning = false;

        Logger.LogDebug("Stopping OSDP Server connections...");

        while (true)
        {
            var entries = _connections.ToArray();
            if (entries.Length == 0) break;

            await Task.WhenAll(entries.Select(item => item.Value.Close()));
            await Task.WhenAll(entries.Select(x => x.Key));
        }

        Logger.LogDebug("OSDP Server STOPPED");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Intended to be called by a deriving class whenever it spins off a dedicated
    /// listening loop task for a newly created OsdpConnection
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="task"></param>
    protected void RegisterConnection(OsdpConnection connection, Task task)
    {
        Task.Run(async () =>
        {
            _connections.TryAdd(task, connection);
            if (!IsRunning) await connection.Close();
            Logger?.LogDebug("New OSDP connection opened - {}", _connections.Count);
            await task;
            _connections.TryRemove(task, out _);
            Logger?.LogDebug("OSDP connection terminated - {}", _connections.Count);
        });
    }


    /// <summary>
    /// Releases the resources used by the <see cref="OsdpServer"/> instance.
    /// </summary>
   protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                var _ = Stop();
            }

            _disposedValue = true;
        }
    }
}