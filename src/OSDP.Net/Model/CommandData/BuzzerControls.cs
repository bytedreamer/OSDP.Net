using System.Collections.Generic;

namespace OSDP.Net.Model.CommandData
{
    public class BuzzerControl
    {
        public BuzzerControl(byte readerNumber, ToneCode toneCode, byte onTime, byte offTime, byte count)
        {
            ReaderNumber = readerNumber;
            ToneCode = toneCode;
            OnTime = onTime;
            OffTime = offTime;
            Count = count;
        }

        public byte ReaderNumber { get; }
        public ToneCode ToneCode { get; }
        public byte OnTime { get; private set; }
        public byte OffTime { get; private set; }
        public byte Count { get; private set; }

        public IEnumerable<byte> BuildData()
        {
            return new[] {ReaderNumber, (byte) ToneCode, OnTime, OffTime, Count};
        }
    }

    public enum ToneCode
    {
        None = 0x00,
        Off = 0x01,
        Default = 0x02
    }
}