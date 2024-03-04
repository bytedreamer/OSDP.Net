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
}
