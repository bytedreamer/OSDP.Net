using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OSDP.Net.Connections
{
    /// <summary>
    /// Base OSDP connection class
    /// </summary>
    public abstract class OsdpConnection : IOsdpConnection
    {
        private bool _disposedValue;

        /// <summary>
        /// Creates an instance of OsdpConnection
        /// </summary>
        /// <param name="baudRate">Baud rate for OSDP comms</param>
        protected OsdpConnection(int baudRate)
        {
            BaudRate = baudRate;
        }   

        /// <inheritdoc/>
        public int BaudRate { get; }

        /// <inheritdoc/>
        public virtual bool IsOpen { get; protected set; }

        /// <inheritdoc/>
        public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <inheritdoc/>
        public abstract Task Close();

        /// <inheritdoc/>
        public abstract Task Open();

        /// <inheritdoc/>
        public abstract Task<int> ReadAsync(byte[] buffer, CancellationToken token);

        /// <inheritdoc/>
        public abstract Task WriteAsync(byte[] buffer);

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Close();
                }

                _disposedValue = true;
            }
        }
    }


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
        public OsdpServer(ILoggerFactory loggerFactory = null)
        {
            LoggerFactory = loggerFactory;
            Logger = loggerFactory?.CreateLogger<OsdpServer>();
        }

        /// <inheritdoc/>
        public bool IsRunning { get; protected set; }

        /// <inheritdoc/>
        public int ConnectionCount { get => _connections.Count; }

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

        /// <inheritdoc/>
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
}
