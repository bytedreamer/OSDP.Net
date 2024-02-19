using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OSDP.Net.Connections
{
    /// <summary>
    /// Connects using TCP/IP connection to a server.</summary>
    public class TcpClientOsdpConnection : IOsdpConnection
    {
        private readonly int _portNumber;
        private readonly string _server;
        private TcpClient _tcpClient;
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpClientOsdpConnection"/> class.
        /// </summary>
        /// <param name="server">The server name or IP address.</param>
        /// <param name="portNumber">The port number.</param>
        /// <param name="baudRate">The baud rate.</param>
        public TcpClientOsdpConnection(string server, int portNumber, int baudRate)
        {
            _tcpClient = new TcpClient();
            _server = server;
            _portNumber = portNumber;
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
                return tcpClient?.Connected ?? false;
            }
        }

        /// <inheritdoc />
        public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <inheritdoc />
        public void Open()
        {
            Close();

            _tcpClient = new TcpClient {NoDelay = true};
            _tcpClient.Connect(_server, _portNumber);
        }

        /// <inheritdoc />
        public void Close()
        {
            var tcpClient = _tcpClient;
            _tcpClient = null;
            if (_tcpClient?.Connected ?? false) tcpClient?.GetStream().Close();
            tcpClient?.Close();
        }

        /// <inheritdoc />
        public async Task WriteAsync(byte[] buffer)
        {
            if (!IsOpen) Open();

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
            return $"{_server}:{_portNumber}";
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

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}