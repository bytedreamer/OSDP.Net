using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDP.Net.Messages
{
    public class Reply : Message
    {
        private readonly Guid _connectionId;
        private readonly IReadOnlyList<byte> _data;

        public Reply(IReadOnlyList<byte> data, Guid connectionId)
        {
            _data = data;
            _connectionId = connectionId;
        }

        private byte Address => (byte) (_data[1] & 0x7F);
        private ReplyType Type => (ReplyType) _data[5];

        public bool IsValidReply(Command command)
        {
            return IsCorrectAddress(command) && IsDataCorrect(command);
        }

        public override string ToString()
        {
            return $"Connection ID: {_connectionId} Address: {Address} Type: {Type}";
        }

        private bool IsCorrectAddress(Command command)
        {
            return command.Address == Address;
        }

        private bool IsDataCorrect(Command command)
        {
            return command.Control.UseCrc
                ? CalculateCrc(_data.Take(_data.Count - 2).ToArray()) ==
                  ConvertBytesToShort(_data.Skip(_data.Count - 2).Take(2).ToArray())
                : CalculateChecksum(_data.Take(_data.Count - 1).ToArray()) == _data.Last();
        }
    }

    public enum ReplyType
    {
        Ack = 0x40,
        Nak = 0x41,
        PdIdReport = 0x45,
        PdCapabilitiesReport = 0x46,
        LocalStatusReport = 0x48,
        InputStatusReport = 0x49,
        OutputReaderReport = 0x4A,
        ReaderStatusReport = 0x4B,
        RawReaderData = 0x50,
        FormattedReaderData = 0x51,
        KeypadData = 0x53,
        PdCommunicationsConfigurationReport = 0x54,
        BiometricData = 0x57,
        BiometricMatchResult = 0x58,
        CrypticData = 0x76,
        InitialRMac = 0x78,
        Busy = 0x79,
        ManufactureSpecific = 0x90
    }
}