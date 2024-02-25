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
    public abstract class OsdpConnection : IOsdpConnection
    {
        private bool _disposedValue;

        protected OsdpConnection(int baudRate)
        {
            BaudRate = baudRate;
        }   

        public int BaudRate { get; }

        public virtual bool IsOpen { get; protected set; }

        public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromMilliseconds(200);

        public abstract Task Close();
        
        public abstract Task Open();
        
        public abstract Task<int> ReadAsync(byte[] buffer, CancellationToken token);
        
        public abstract Task WriteAsync(byte[] buffer);

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

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


    public abstract class OsdpServer : IOsdpServer
    {
        private bool _disposedValue;
        private readonly ConcurrentDictionary<Task, OsdpConnection> _connections = new();
        private readonly ILogger _logger;

        public OsdpServer(ILoggerFactory loggerFactory = null)
        {
            _logger = loggerFactory?.CreateLogger<OsdpServer>();
        }

        public bool IsRunning { get; protected set; }

        public int ConnectionCount { get => _connections.Count; }

        public abstract Task Start(Func<IOsdpConnection, Task> newConnectionHandler);

        public virtual async Task Stop()
        {
            IsRunning = false;

            _logger.LogDebug("Stopping OSDP Server connections...");

            while (true)
            {
                var entries = _connections.ToArray();
                if (entries.Length == 0) break;

                foreach (var item in entries)
                {
                    item.Value.Close();
                }

                await Task.WhenAll(entries.Select(x => x.Key));
            }

            _logger.LogDebug("OSDP Server STOPPED");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void RegisterConnection(OsdpConnection connection, Task task)
        {
            Task.Run(async () =>
            {
                _logger?.LogDebug("New OSDP connection opened - {}", _connections.Count + 1);

                _connections.TryAdd(task, connection);
                if (!IsRunning) connection.Close();
                await task;
                _logger?.LogDebug("OSDP connection terminated - {}", _connections.Count - 1);
                _connections.TryRemove(task, out _);
            });
        }

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
