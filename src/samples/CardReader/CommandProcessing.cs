using System.Collections.Concurrent;
using OSDP.Net;
using OSDP.Net.Model;
using OSDP.Net.Model.ReplyData;

namespace CardReader;


/// <inheritdoc />
internal class CommandProcessing : ICommandProcessing
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

    /// <inheritdoc />
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
