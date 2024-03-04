using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OSDP.Net.Connections;

internal sealed class TcpServerOsdpConnection2 : OsdpConnection
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
        catch (Exception exception) 
        {
            if (exception is not OperationCanceledException && IsOpen)
            {
                if (exception is IOException && exception.InnerException is SocketException)
                {
                    _logger?.LogInformation("Error reading tcp stream: {ExceptionMessage}", exception.Message);
                } 
                else
                {
                    _logger?.LogWarning(exception, "Error reading tcp stream");
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
        
    /// <inheritdoc />
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