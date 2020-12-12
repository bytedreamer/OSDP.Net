using System.Collections.Generic;

namespace OSDP.Net.Model.CommandData
{
    public class ReaderBuzzerControl
    {
        public ReaderBuzzerControl(byte readerNumber, ToneCode toneCode, byte onTime, byte offTime, byte count)
        {
            ReaderNumber = readerNumber;
            ToneCode = toneCode;
            OnTime = onTime;
            OffTime = offTime;
            Count = count;
        }

        public byte ReaderNumber { get; }
        public ToneCode ToneCode { get; }
        public byte OnTime { get; }
        public byte OffTime { get; }
        public byte Count { get;  }

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