using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OSDP.Net.Connections
{
    public class TcpServerOsdpConnection : IOsdpConnection
    {
        private readonly TcpListener _listener;
        private TcpClient _tcpClient;

        public TcpServerOsdpConnection(int portNumber, int baudRate)
        {
            _listener = TcpListener.Create(portNumber);
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

        public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public void Open()
        {
            _listener.Start();
            _tcpClient = _listener.AcceptTcpClient();
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
                await tcpClient.GetStream().WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            }
        }

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