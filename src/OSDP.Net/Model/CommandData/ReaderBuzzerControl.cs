using System;
using System.Collections.Generic;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Command data to control the buzzer on a PD.
    /// </summary>
    public class ReaderBuzzerControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReaderBuzzerControl"/> class.
        /// </summary>
        /// <param name="readerNumber">The reader number starting at 0.</param>
        /// <param name="toneCode">The tone code.</param>
        /// <param name="onTime">The on time in units of 100ms.</param>
        /// <param name="offTime">The off time in units of 100ms.</param>
        /// <param name="count">The number of times to repeat the on/off cycle.</param>
        public ReaderBuzzerControl(byte readerNumber, ToneCode toneCode, byte onTime, byte offTime, byte count)
        {
            ReaderNumber = readerNumber;
            ToneCode = toneCode;
            OnTime = onTime;
            OffTime = offTime;
            Count = count;
        }

        /// <summary>
        /// Gets the reader number starting at 0.
        /// </summary>
        public byte ReaderNumber { get; }

        /// <summary>
        /// Gets the tone code.
        /// </summary>
        public ToneCode ToneCode { get; }

        /// <summary>
        /// Gets the on time in units of 100ms.
        /// </summary>
        public byte OnTime { get; }

        /// <summary>
        /// Gets the off time in units of 100ms.
        /// </summary>
        public byte OffTime { get; }

        /// <summary>
        /// Gets the number of times to repeat the on/off cycle.
        /// </summary>
        public byte Count { get;  }

        internal IEnumerable<byte> BuildData()
        {
            return new[] {ReaderNumber, (byte) ToneCode, OnTime, OffTime, Count};
        }
    }

    /// <summary>
    /// Tone codes values.
    /// </summary>
    public enum ToneCode
    {
        [Obsolete("This no tone code is obsolete.", false)]
        None = 0x00,

        /// <summary>
        /// Turn off the tone
        /// </summary>
        Off = 0x01,

        /// <summary>
        /// Turn on the default tone
        /// </summary>
        Default = 0x02
    }
}