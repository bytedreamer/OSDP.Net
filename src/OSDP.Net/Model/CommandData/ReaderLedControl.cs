using System.Collections.Generic;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    public class ReaderLedControls
    {
        public ReaderLedControls(IEnumerable<ReaderLedControl> controls)
        {
            Controls = controls;
        }

        public IEnumerable<ReaderLedControl> Controls { get; }

        public IEnumerable<byte> BuildData()
        {
            var data = new List<byte>();
            foreach (var readerLedControl in Controls)
            {
                data.AddRange(readerLedControl.BuildData());
            }

            return data;
        }
    }
    
    public class ReaderLedControl
    {
        public ReaderLedControl(
            byte readerNumber, 
            byte ledNumber, 
            TemporaryReaderControlCode temporaryMode, 
            byte temporaryOnTime, 
            byte temporaryOffTime,
            LedColor temporaryOnColor,
            LedColor temporaryOffColor,
            ushort temporaryTimer,
            PermanentReaderControlCode permanentMode,
            byte permanentOnTime,
            byte permanentOffTime,
            LedColor permanentOnColor,
            LedColor permanentOffColor)
        {
            ReaderNumber = readerNumber;
            LedNumber = ledNumber;
            TemporaryMode = temporaryMode;
            TemporaryOnTime = temporaryOnTime;
            TemporaryOffTime = temporaryOffTime;
            TemporaryOnColor = temporaryOnColor;
            TemporaryOffColor = temporaryOffColor;
            TemporaryTimer = temporaryTimer;
            PermanentMode = permanentMode;
            PermanentOnTime = permanentOnTime;
            PermanentOffTime = permanentOffTime;
            PermanentOnColor = permanentOnColor;
            PermanentOffColor = permanentOffColor;
        }

        public byte ReaderNumber { get; }
        public byte LedNumber { get; }
        public TemporaryReaderControlCode TemporaryMode { get; }
        public byte TemporaryOnTime { get; }
        public byte TemporaryOffTime { get; }
        public LedColor TemporaryOnColor { get; }
        public LedColor TemporaryOffColor { get; }
        public ushort TemporaryTimer { get; }
        public PermanentReaderControlCode PermanentMode { get; }
        public byte PermanentOnTime { get; }
        public byte PermanentOffTime { get; }
        public LedColor PermanentOnColor { get; }
        public LedColor PermanentOffColor { get; }

        public IEnumerable<byte> BuildData()
        {
            var temporaryTimerBytes = Message.ConvertShortToBytes(TemporaryTimer);
            
            return new[]
            {
                ReaderNumber, 
                LedNumber,
                (byte) TemporaryMode, 
                TemporaryOnTime,
                TemporaryOffTime,
                (byte) TemporaryOnColor,
                (byte) TemporaryOffColor,
                temporaryTimerBytes[0], 
                temporaryTimerBytes[1],
                (byte) PermanentMode,
                PermanentOnTime,
                PermanentOffTime,
                (byte) PermanentOnColor,
                (byte) PermanentOffColor
            };
        }
    }

    public enum TemporaryReaderControlCode
    {
        Nop = 0x00,
        CancelAnyTemporaryAndDisplayPermanent = 0x01,
        SetTemporaryAndStartTimer = 0x02
    }
    
    public enum PermanentReaderControlCode
    {
        Nop = 0x00,
        SetPermanentState = 0x01
    }

    public enum LedColor
    {
        Black = 0,
        Red = 1,
        Green =  2,
        Amber = 3,
        Blue = 4
    }
}