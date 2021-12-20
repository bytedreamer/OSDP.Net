using System.Collections.Generic;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Contains data to control the color of a single LED on a PD.
    /// </summary>
    public class ReaderLedControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReaderLedControl"/> class.
        /// </summary>
        /// <param name="readerNumber">The reader number starting at 0.</param>
        /// <param name="ledNumber">The LED number.</param>
        /// <param name="temporaryMode">The temporary mode.</param>
        /// <param name="temporaryOnTime">The temporary on time in units of 100ms. NOTE: Both On and Off time shouldn't be zero, set this to one if not needed.</param>
        /// <param name="temporaryOffTime">The temporary off time in units of 100ms.</param>
        /// <param name="temporaryOnColor">Color of the temporary on.</param>
        /// <param name="temporaryOffColor">Color of the temporary off.</param>
        /// <param name="temporaryTimer">The temporary timer in units of 100ms.</param>
        /// <param name="permanentMode">The permanent mode.</param>
        /// <param name="permanentOnTime">The permanent on time in units of 100ms. NOTE: Both On and Off time shouldn't be zero, set this to one if not needed.</param>
        /// <param name="permanentOffTime">The permanent off time in units of 100ms.</param>
        /// <param name="permanentOnColor">Color of the permanent on.</param>
        /// <param name="permanentOffColor">Color of the permanent off.</param>
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

        /// <summary>Gets the reader number starting at 0.</summary>
        public byte ReaderNumber { get; }

        /// <summary>
        /// Gets the LED number.
        /// </summary>
        public byte LedNumber { get; }

        /// <summary>
        /// Gets the temporary mode.
        /// </summary>
        public TemporaryReaderControlCode TemporaryMode { get; }

        /// <summary>
        /// Gets the temporary on time in units of 100ms.
        /// </summary>
        public byte TemporaryOnTime { get; }

        /// <summary>
        /// Gets the temporary off time in units of 100ms.
        /// </summary>
        public byte TemporaryOffTime { get; }

        /// <summary>
        /// Gets the color of the temporary on.
        /// </summary>
        public LedColor TemporaryOnColor { get; }

        /// <summary>
        /// Gets the color of the temporary off.
        /// </summary>
        public LedColor TemporaryOffColor { get; }

        /// <summary>
        /// Gets the temporary timer  in units of 100ms.
        /// </summary>
        public ushort TemporaryTimer { get; }

        /// <summary>
        /// Gets the permanent mode.
        /// </summary>
        public PermanentReaderControlCode PermanentMode { get; }

        /// <summary>
        /// Gets the permanent on time in units of 100ms.
        /// </summary>
        public byte PermanentOnTime { get; }

        /// <summary>
        /// Gets the permanent off time in units of 100ms.
        /// </summary>
        public byte PermanentOffTime { get; }

        /// <summary>
        /// Gets the color of the permanent on.
        /// </summary>
        public LedColor PermanentOnColor { get; }

        /// <summary>
        /// Gets the color of the permanent off.
        /// </summary>
        public LedColor PermanentOffColor { get; }

        internal IEnumerable<byte> BuildData()
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

    /// <summary>
    /// Temporary control code values.
    /// </summary>
    public enum TemporaryReaderControlCode
    {
        /// <summary>
        /// Do not alter this LED's temporary settings. The remaining values of the temporary settings record are ignored.
        /// </summary>
        Nop = 0x00,

        /// <summary>
        /// Cancel any temporary operation and display this LED's permanent state immediately.
        /// </summary>
        CancelAnyTemporaryAndDisplayPermanent = 0x01,

        /// <summary>
        /// Set the temporary state as given and start timer immediately.
        /// </summary>
        SetTemporaryAndStartTimer = 0x02
    }

    /// <summary>
    /// Permanent control code values.
    /// </summary>
    public enum PermanentReaderControlCode
    {
        /// <summary>
        /// Do not alter this LED's permanent settings. The remaining values of the temporary settings record are ignored.
        /// </summary>
        Nop = 0x00,

        /// <summary>
        /// Set the permanent state as given.
        /// </summary>
        SetPermanentState = 0x01
    }

    /// <summary>
    /// Color values.
    /// </summary>
    public enum LedColor
    {
#pragma warning disable CS1591
        Black = 0,
        Red = 1,
        Green =  2,
        Amber = 3,
        Blue = 4,
        Magenta = 5,
        Cyan = 6,
        White = 7
#pragma warning restore CS1591
    }
}