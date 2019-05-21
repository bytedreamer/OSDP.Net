using System.Threading.Tasks;

namespace OSDP.Net
{
    public interface IOsdpConnection
    {
        bool IsOpen { get; }
        
        void Open();
        void Close();

        Task Write(byte[] command);
        Task<int> Read(byte[] readBuffer);
    }
}