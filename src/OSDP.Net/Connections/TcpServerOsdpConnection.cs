using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OSDP.Net.Connections
{
    /// <summary>Listens for a TCP/IP connection from a server.</summary>
    public class TcpServerOsdpConnection : IOsdpConnection
    {
        private readonly TcpListener _listener;
        private TcpClient _tcpClient;
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpServerOsdpConnection"/> class.
        /// </summary>
        /// <param name="portNumber">The port number.</param>
        /// <param name="baudRate">The baud rate.</param>
        public TcpServerOsdpConnection(int portNumber, int baudRate)
        {
            _listener = TcpListener.Create(portNumber);
            BaudRate = baudRate;
        }

        /// <inheritdoc />
        public int BaudRate { get; }

        /// <inheritdoc />
        public bool IsOpen
        {
            get
            {
                var tcpClient = _tcpClient;
                return tcpClient is { Connected: true };
            }
        }

        /// <inheritdoc />
        public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <inheritdoc />
        public void Open()
        {
            _listener.Start();
            var newTcpClient = _listener.AcceptTcpClient();

            Close();

            _tcpClient = newTcpClient;
        }

        /// <inheritdoc />
        public void Close()
        {
            var tcpClient = _tcpClient;
            _tcpClient = null;
            if (tcpClient?.Connected ?? false) tcpClient?.GetStream().Close();
            tcpClient?.Close();
        }

        /// <inheritdoc />
        public async Task WriteAsync(byte[] buffer)
        {
            var tcpClient = _tcpClient;
            if (tcpClient != null)
            {
                await tcpClient.GetStream().WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
        {
            var tcpClient = _tcpClient;
            if (tcpClient != null)
            {
                return await tcpClient.GetStream().ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
            }

            return 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _listener?.LocalEndpoint.ToString();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }


    class TcpServerOsdpConnection2 : IOsdpConnection
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private bool disposedValue;

        public TcpServerOsdpConnection2(TcpClient tcpClient, int baudRate)
        {
            IsOpen = true;
            BaudRate = baudRate;
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
        }

        public int BaudRate { get; }

        public bool IsOpen { get; private set; }

        public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromMilliseconds(200);

        public async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
        {
            try
            {
                return await _stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
            }
            catch (Exception ex) 
            {
                if (!(ex is OperationCanceledException)) IsOpen = false;
                return 0;
            }
        }

        public async Task WriteAsync(byte[] buffer)
        {
            try
            {
                await _stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // TODO: Add logging?
                IsOpen = false;
            }
        }

        public void Open() => throw new NotSupportedException();

        public void Close() => Dispose();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _stream.Dispose();
                    _tcpClient.Dispose();
                }

                _stream = null;
                _tcpClient = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }


    public class TcpOsdpServer : IOsdpServer
    {
        private TcpListener _listener;
        private int _baudRate;
        private bool _disposedValue;

        public TcpOsdpServer(int portNumber, int baudRate)
        {
            _listener = TcpListener.Create(portNumber);
            _baudRate = baudRate;
        }

        public bool IsRunning {get; private set;}

        public void Start(Func<IOsdpConnection, Task> newConnectionHandler)
        {
            if (IsRunning) return;

            IsRunning = true;
            _listener.Start();

            Task.Run(async () =>
            {
                while (IsRunning)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    if (client != null)
                    {
                        TcpServerOsdpConnection2 connection = new(client, _baudRate);

                        // Intentionally let this function spin off on its own so that the caller
                        // can work with the connection and let it go on their own
                        var _ = newConnectionHandler(connection);
                    }
                }
            });
        }

        public void Stop()
        {
            IsRunning = false;
            _listener.Stop();
        }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Stop();
                }

                _listener = null;
                _disposedValue = true;
            }
        }
    }
}