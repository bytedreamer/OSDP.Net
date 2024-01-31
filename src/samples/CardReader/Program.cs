using System.Collections;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Model;
using OSDP.Net.Model.ReplyData;


// var connection = new SerialPortOsdpConnection("COM3", 9600);
var connection = new TcpServerOsdpConnection(8200, 9600);
using var device = new MySampleDevice();

device.StartListening(connection);

var _ = Task.Factory.StartNew(() =>
{
    var cardNumber = new BitArray(26);
    
    while (true)
    {
        // ReSharper disable once AccessToDisposedClosure
        if (!device.IsConnected) continue;
        
        device.EnqueuePollReply(new RawCardData(0, FormatCode.NotSpecified, cardNumber));
        return;
    }
});

Console.ReadKey();

device.StopListening();


class MySampleDevice : Device
{
    protected override PayloadData HandleIdReport()
    {
        return new DeviceIdentification(new byte[] { 0x00, 0x00, 0x00 }, 0, 1, 0, 0, 0, 0);
    }
}
