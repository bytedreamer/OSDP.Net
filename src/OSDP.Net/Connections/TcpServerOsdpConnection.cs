using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OSDP.Net.Connections
{
    /// <summary>Listens for a TCP/IP connection from a server.</summary>
    public class TcpServerOsdpConnection : OsdpConnection
    {
        private readonly TcpListener _listener;
        private TcpClient _tcpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpServerOsdpConnection"/> class.
        /// </summary>
        /// <param name="portNumber">The port number.</param>
        /// <param name="baudRate">The baud rate.</param>
        public TcpServerOsdpConnection(int portNumber, int baudRate) : base(baudRate)
        {
            _listener = TcpListener.Create(portNumber);
        }

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
        public override async Task Open()
        {
            _listener.Start();
            var newTcpClient = await _listener.AcceptTcpClientAsync();

            Close();

            _tcpClient = newTcpClient;
        }

        /// <inheritdoc />
        public override Task Close()
        {
            var tcpClient = _tcpClient;
            _tcpClient = null;
            if (tcpClient?.Connected ?? false) tcpClient?.GetStream().Close();
            tcpClient?.Close();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override async Task WriteAsync(byte[] buffer)
        {
            var tcpClient = _tcpClient;
            if (tcpClient != null)
            {
                await tcpClient.GetStream().WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
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
    }


    internal class TcpServerOsdpConnection2 : OsdpConnection
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;

        public TcpServerOsdpConnection2(TcpClient tcpClient, int baudRate) : base(baudRate)
        {
            IsOpen = true;
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
        }

        public override async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
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

        public override async Task WriteAsync(byte[] buffer)
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

        public override Task Open() => throw new NotSupportedException();

        public override Task Close()
        {
            _stream?.Dispose();
            _tcpClient?.Dispose();
            _stream = null;
            _tcpClient = null;
            return Task.CompletedTask;
        }
    }


    public class TcpOsdpServer : OsdpServer
    {
        private TcpListener _listener;
        private int _baudRate;
        private bool _disposedValue;

        public TcpOsdpServer(
            int portNumber, int baudRate, ILoggerFactory loggerFactory = null) : base(loggerFactory)
        {
            _listener = TcpListener.Create(portNumber);
            _baudRate = baudRate;
        }

        public override Task Start(Func<IOsdpConnection, Task> newConnectionHandler)
        {
            if (IsRunning) return Task.CompletedTask;

            IsRunning = true;
            _listener.Start();

            Task.Run(async () =>
            {
                while (IsRunning)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    if (client != null)
                    {
                        var connection = new TcpServerOsdpConnection2(client, _baudRate);
                        var task = newConnectionHandler(connection);
                        RegisterConnection(connection, task);
                    }
                }
            });

            return Task.CompletedTask;
        }

        public override Task Stop()
        {
            IsRunning = false;
            _listener.Stop();
            return base.Stop();
        }
    }
}
