using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OSDP.Net.Connections
{
    public class TcpClientOsdpConnection : IOsdpConnection
    {
        private readonly int _portNumber;
        private readonly string _server;
        private TcpClient _tcpClient;

        public TcpClientOsdpConnection(string server, int portNumber, int baudRate)
        {
            _tcpClient = new TcpClient();
            _server = server;
            _portNumber = portNumber;
            BaudRate = baudRate;
        }

        public int BaudRate { get; }

        public bool IsOpen
        {
            get
            {
                var tcpClient = _tcpClient;
                return tcpClient != null && tcpClient.Connected;
            }
        }

        public void Open()
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(_server, _portNumber);
        }

        public void Close()
        {
            var tcpClient = _tcpClient;
            _tcpClient = null;
            tcpClient?.Close();
        }

        public async Task WriteAsync(byte[] buffer)
        {
            var tcpClient = _tcpClient;
            if (tcpClient != null)
            {
                await tcpClient.GetStream().WriteAsync(buffer, 0, buffer.Length);
            }
        }

        public async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
        {
            var tcpClient = _tcpClient;
            if (tcpClient != null)
            {
                return await tcpClient.GetStream().ReadAsync(buffer, 0, buffer.Length, token);
            }

            return 0;
        }
    }
}