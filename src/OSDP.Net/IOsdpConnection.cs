using System.Threading;
using System.Threading.Tasks;

namespace OSDP.Net
{
    public interface IOsdpConnection
    {
         /// <summary>
         /// Is the connection open and ready to communicate
         /// </summary>
        bool IsOpen { get; }
        
         /// <summary>
         /// Open the connection for communications
         /// </summary>
        void Open();
        
         /// <summary>
         /// Close the connection for communications
         /// </summary>
        void Close();

         /// <summary>
         /// Write to connection
         /// </summary>
         /// <param name="buffer">Array of bytes to write</param>
        Task WriteAsync(byte[] buffer);

         /// <summary>
         /// Read from connection
         /// </summary>
         /// <param name="buffer">Array of bytes to read</param>
         /// <param name="token">Cancellation token to end reading of bytes</param>
         /// <returns>Number of actual bytes read</returns>
         Task<int> ReadAsync(byte[] buffer, CancellationToken token);
    }
}