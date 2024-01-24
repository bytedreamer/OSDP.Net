using System.Collections;
using System.Collections.Concurrent;
using System.Numerics;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Model;
using OSDP.Net.Model.ReplyData;

var portName = "/dev/ttyUSB0";
var baudRate = 9600;

var outgoingReplies = new ConcurrentQueue<PayloadData>();
var connection = new SerialPortOsdpConnection(portName, baudRate);
using var device = new Device(0, true, false, []);
device.StartListening(connection, new CommandProcessing(outgoingReplies));

Task.Factory.StartNew(() =>
{
    // The card format number for this example is 128 bit (as raw card data)
    var cardNumberInt = BigInteger.Parse("30313233343536373839303030303032",System.Globalization.NumberStyles.HexNumber);
    var cardNumberBitString = BigIntegerToBinaryString(cardNumberInt).PadLeft(128, '0');
    var cardNumber = new BitArray(cardNumberBitString.Select(c => c == '1').ToArray());

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
return;

static string BigIntegerToBinaryString(BigInteger value)
{
    var result = "";

    while (value > 0)
    {
        result = value % 2 + result;
        value /= 2;
    }

    return string.IsNullOrEmpty(result) ? "0" : result;
}

/// <inheritdoc />
class CommandProcessing : ICommandProcessing
{
    private readonly ConcurrentQueue<PayloadData> _outgoingReplies;

    public CommandProcessing(ConcurrentQueue<PayloadData> outgoingReplies)
    {
        _outgoingReplies = outgoingReplies;
    }

    /// <inheritdoc />
    public PayloadData Poll()
    {
        return _outgoingReplies.TryDequeue(out var replyData) ? replyData : new Ack();
    }

    /// <inheritdoc />
    public PayloadData IdReport()
    {
        return new DeviceIdentification([0x00, 0x00, 0x00], 0, 1, 0, 0, 0, 0);
    }

    public PayloadData PdCap()
    {
        var deviceCapabilities = new DeviceCapabilities(new[]
        {
            new DeviceCapability(CapabilityFunction.CardDataFormat, 1, 0),
            new DeviceCapability(CapabilityFunction.ReaderLEDControl, 1, 0),
            new DeviceCapability(CapabilityFunction.ReaderTextOutput, 0, 0),
            new DeviceCapability(CapabilityFunction.CheckCharacterSupport, 1, 0),
            new DeviceCapability(CapabilityFunction.CommunicationSecurity, 1, 1),
            new DeviceCapability(CapabilityFunction.ReceiveBufferSize, 0, 1),
            new DeviceCapability(CapabilityFunction.OSDPVersion, 2, 0)
        });

        return deviceCapabilities;
    }
}
