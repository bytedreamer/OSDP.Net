using System;
using System.Collections.Generic;
using System.Text;

namespace OSDP.Net.Model.CommandData
{
    public class ReaderTextOutput
    {
        public ReaderTextOutput(byte readerNumber, TextCommand textCommand, byte temporaryTextTime, byte row, byte column, string text)
        {
            ReaderNumber = readerNumber;
            TextCommand = textCommand;
            TemporaryTextTime = temporaryTextTime;
            Row = row;
            Column = column;
            Text = text;
        }

        public byte ReaderNumber { get; }
        public TextCommand TextCommand { get; }
        public byte TemporaryTextTime { get; }
        public byte Row { get; }
        public byte Column { get; }
        public string Text { get; }

        public IEnumerable<byte> BuildData()
        {
            var data = new List<byte>
                {ReaderNumber, (byte) TextCommand, TemporaryTextTime, Row, Column, (byte) Text.Length};
            data.AddRange(Encoding.ASCII.GetBytes(Text.Substring(0, Math.Min(Text.Length, byte.MaxValue))));
            return data;
        }
    }

    public enum TextCommand
    {
        PermanentTextNoWrap = 0x01,
        PermanentTextWithWrap = 0x02,
        TemporaryTextNoWrap = 0x03,
        TemporaryTextWithWrap = 0x04
    }
}