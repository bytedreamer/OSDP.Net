using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OSDP.Net.Connections
{
    /// <summary>
    /// Connects using TCP/IP connection to a server.</summary>
    public class TcpClientOsdpConnection : OsdpConnection
    {
        private readonly int _portNumber;
        private readonly string _server;
        private TcpClient _tcpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpClientOsdpConnection"/> class.
        /// </summary>
        /// <param name="server">The server name or IP address.</param>
        /// <param name="portNumber">The port number.</param>
        /// <param name="baudRate">The baud rate.</param>
        public TcpClientOsdpConnection(string server, int portNumber, int baudRate) : base(baudRate)
        {
            _tcpClient = new TcpClient();
            _server = server;
            _portNumber = portNumber;
        }

        /// <inheritdoc />
        public override bool IsOpen
        {
            get
            {
                var tcpClient = _tcpClient;
                return tcpClient?.Connected ?? false;
            }
        }

        /// <inheritdoc />
        public override async Task Open()
        {
            await Close();

            _tcpClient = new TcpClient {NoDelay = true};
            await _tcpClient.ConnectAsync(_server, _portNumber);
        }

        /// <inheritdoc />
        public override Task Close()
        {
            var tcpClient = _tcpClient;
            _tcpClient = null;
            if (_tcpClient?.Connected ?? false) tcpClient?.GetStream().Close();
            tcpClient?.Close();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override async Task WriteAsync(byte[] buffer)
        {
            if (!IsOpen) await Open();

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
            return $"{_server}:{_portNumber}";
        }
    }
}
