using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OSDP.Net.Connections
{
    /// <summary>
    /// Initial implementation of TCP server OSDP connection which combines
    /// the listener as well as the accepted connection in a single class.
    /// 
    /// The use of this class might be questionable as TCP/IP protocol 
    /// inherently behaves differently enough that this class has some limitations
    /// which have been addressed by TcpOsdpServer
    /// </summary>
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
        public override bool IsOpen
        {
            get
            {
                var tcpClient = _tcpClient;
                return tcpClient is { Connected: true };
            }
        }

        /// <inheritdoc />
        public override async Task Open()
        {
            _listener.Start();
            var newTcpClient = await _listener.AcceptTcpClientAsync();

            await Close();

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
        private readonly ILogger _logger;
        private TcpClient _tcpClient;
        private NetworkStream _stream;

        public TcpServerOsdpConnection2(
            TcpClient tcpClient, int baudRate, ILoggerFactory loggerFactory) : base(baudRate)
        {
            IsOpen = true;
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
            _logger = loggerFactory?.CreateLogger<TcpServerOsdpConnection2>();
        }

        public override async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
        {
            try
            {
                var bytes = await _stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                if (bytes == 0) { IsOpen = false; }
                return bytes;
            }
            catch (Exception ex) 
            {
                if (ex is not OperationCanceledException && IsOpen)
                {
                    if (ex is IOException && ex.InnerException is SocketException)
                    {
                        _logger?.LogInformation("Error reading tcp stream: {err}", ex.Message);
                    } 
                    else
                    {
                        _logger?.LogWarning(ex, "Error reading tcp stream");
                    }
                    
                    IsOpen = false;
                }
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
                if (IsOpen)
                {
                    _logger?.LogWarning(ex, "Error writing tcp stream");
                    IsOpen = false;
                }
            }
        }

        public override Task Open() => throw new NotSupportedException();

        public override Task Close()
        {
            IsOpen = false;
            _stream?.Dispose();
            _tcpClient?.Dispose();
            _stream = null;
            _tcpClient = null;
            return Task.CompletedTask;
        }
    }


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

            Logger?.LogInformation("Listening on {endpoint} for incoming connections...", _listener.LocalEndpoint);

            Task.Run(async () =>
            {
                while (IsRunning)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    if (client != null)
                    {
                        var connection = new TcpServerOsdpConnection2(client, _baudRate, LoggerFactory);
                        var task = newConnectionHandler(connection);
                        RegisterConnection(connection, task);
                    }
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
}
