using System.Collections;
using System.Collections.Concurrent;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Model.ReplyData;

var outgoingReplies = new ConcurrentQueue<ReplyData>();

var connection = new SerialPortOsdpConnection("COM3", 9600);
using var device = new DeviceProxy(0, true, false, []);
device.StartListening(connection, new CommandProcessing(outgoingReplies));

var _ = Task.Factory.StartNew(() =>
{
    var cardNumber = new BitArray(26);
    
    while (true)
    {
        // ReSharper disable once AccessToDisposedClosure
        if (!device.IsConnected) continue;
        
        outgoingReplies.Enqueue(new RawCardData(0, FormatCode.NotSpecified, cardNumber));
        return;
    }
});

Console.ReadKey();

device.StopListening();

/// <inheritdoc />
class CommandProcessing : ICommandProcessing
{
    private readonly ConcurrentQueue<ReplyData> _outgoingReplies;

    public CommandProcessing(ConcurrentQueue<ReplyData> outgoingReplies)
    {
        _outgoingReplies = outgoingReplies;
    }

    /// <inheritdoc />
    public ReplyData Poll()
    {
        return _outgoingReplies.TryDequeue(out var replyData) ? replyData : new Ack();
    }

    /// <inheritdoc />
    public ReplyData IdReport()
    {
        return new DeviceIdentification(new byte[] { 0x00, 0x00, 0x00 }, 0, 1, 0, 0, 0, 0);
    }
}