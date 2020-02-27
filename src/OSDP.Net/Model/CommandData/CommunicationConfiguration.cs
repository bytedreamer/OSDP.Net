using System.Collections.Generic;
using System.Linq;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    public class CommunicationConfiguration
    {
        public CommunicationConfiguration(byte address, int baudRate)
        {
            Address = address;
            BaudRate = baudRate;
        }

        public byte Address { get; }
        public int BaudRate { get; }

        public IEnumerable<byte> BuildData()
        {
            var baudRateBytes = Message.ConvertIntToBytes(BaudRate).ToArray();
            
            return new[]
            {
                Address,
                baudRateBytes[0],
                baudRateBytes[1],
                baudRateBytes[2],
                baudRateBytes[3]
            };
        }
    }
}