using System;
using System.Collections.Generic;
using System.Text;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Command data for sending text to be shown on a PD.
    /// </summary>
    public class ReaderTextOutput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReaderTextOutput"/> class.
        /// </summary>
        /// <param name="readerNumber">The reader number starting at 0.</param>
        /// <param name="textCommand">The text command for displaying.</param>
        /// <param name="temporaryTextTime">The temporary text time in units of 100ms.</param>
        /// <param name="row">The row where the first character will be displayed. 0x01 = top row.</param>
        /// <param name="column">The column where the first character will be displayed 0x01 = leftmost column.</param>
        /// <param name="text">The text to display.</param>
        public ReaderTextOutput(byte readerNumber, TextCommand textCommand, byte temporaryTextTime, byte row, byte column, string text)
        {
            ReaderNumber = readerNumber;
            TextCommand = textCommand;
            TemporaryTextTime = temporaryTextTime;
            Row = row;
            Column = column;
            Text = text;
        }

        /// <summary>
        /// Gets the reader number starting at 0.
        /// </summary>
        public byte ReaderNumber { get; }

        /// <summary>
        /// Gets the text command for displaying.
        /// </summary>
        public TextCommand TextCommand { get; }

        /// <summary>
        /// Gets the temporary text time in units of 100ms.
        /// </summary>
        public byte TemporaryTextTime { get; }

        /// <summary>Gets the row where the first character will be displayed. 0x01 = top row.</summary>
        public byte Row { get; }

        /// <summary>The column where the first character will be displayed 0x01 = leftmost column.</summary>
        public byte Column { get; }

        /// <summary>
        /// Gets the text to display.
        /// </summary>
        public string Text { get; }

        internal IEnumerable<byte> BuildData()
        {
            var data = new List<byte>
                {ReaderNumber, (byte) TextCommand, TemporaryTextTime, Row, Column, (byte) Text.Length};
            data.AddRange(Encoding.ASCII.GetBytes(Text.Substring(0, Math.Min(Text.Length, byte.MaxValue))));
            return data;
        }
    }

    /// <summary>
    /// Text command for displaying values.
    /// </summary>
    public enum TextCommand
    {
        /// <summary>
        /// Display text permanently with no wrap
        /// </summary>
        PermanentTextNoWrap = 0x01,

        /// <summary>
        /// Display text permanently with wrap
        /// </summary>
        PermanentTextWithWrap = 0x02,

        /// <summary>
        /// Display text temporarily with no wrap
        /// </summary>
        TemporaryTextNoWrap = 0x03,

        /// <summary>
        /// Display text temporarily with wrap
        /// </summary>
        TemporaryTextWithWrap = 0x04
    }
}