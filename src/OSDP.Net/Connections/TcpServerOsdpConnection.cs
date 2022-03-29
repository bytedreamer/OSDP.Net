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
            if (_tcpClient?.Connected ?? false) tcpClient?.GetStream().Close();
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
    }
}