using System;
using System.Collections.Generic;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Contains data to control a single output on a PD.
    /// </summary>
    public class OutputControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OutputControl"/> class.
        /// </summary>
        /// <param name="outputNumber">The output number.</param>
        /// <param name="outputControlCode">The output control code.</param>
        /// <param name="timer">The timer in units of 100ms.</param>
        public OutputControl(byte outputNumber, OutputControlCode outputControlCode, ushort timer)
        {
            OutputNumber = outputNumber;
            OutputControlCode = outputControlCode;
            Timer = timer;
        }

        /// <summary>
        /// Gets the output number.
        /// </summary>
        public byte OutputNumber { get; }

        /// <summary>
        /// Gets the output control code.
        /// </summary>
        public OutputControlCode OutputControlCode { get; }

        /// <summary>
        /// Gets the timer in units of 100ms.
        /// </summary>
        public ushort Timer { get; }

        /// <summary>
        /// Builds the payload data array.
        /// </summary>
        /// <returns>The payload data</returns>
        public IEnumerable<byte> BuildData()
        {
            var timerBytes = Message.ConvertShortToBytes(Timer);
            
            return new[] {OutputNumber, (byte) OutputControlCode, timerBytes[0], timerBytes[1]};
        }
        
        /// <summary>Parses the message payload bytes</summary>
        /// <param name="data">Message payload as bytes</param>
        /// <returns>An instance of OutputControl representing the message payload</returns>
        public static OutputControl ParseData(ReadOnlySpan<byte> data)
        {
            return new OutputControl(
                data[0], (OutputControlCode)data[1],
                Message.ConvertBytesToUnsignedShort(data.Slice(2)));
        }

        /// <inheritdoc/>
        public override string ToString() => ToString(0);

        /// <summary>
        /// Returns a string representation of the current object
        /// </summary>
        /// <param name="indent">Number of ' ' chars to add to beginning of every line</param>
        /// <returns>String representation of the current object</returns>
        public string ToString(int indent)
        {
            var padding = new string(' ', indent);
            var sb = new StringBuilder();
            sb.AppendLine($"{padding} Output #: {OutputNumber}");
            sb.AppendLine($"{padding}Ctrl Code: {OutputControlCode}");
            sb.AppendLine($"{padding}    Timer: {Timer}");
            return sb.ToString();
        }
    }
}
