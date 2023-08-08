using NUnit.Framework;
using OSDP.Net.Utilities;
using System.Linq;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Tests.Model.CommandData
{
    internal class ReaderTextOutputTest
    {
        [Test]
        public void ParseData()
        {
            var inputData = BinaryUtils.HexToBytes("00-02-00-01-01-18-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A").ToArray();

            var actual = ReaderTextOutput.ParseData(inputData);

            Assert.That(actual.ReaderNumber, Is.EqualTo(0));
            Assert.That(actual.TextCommand, Is.EqualTo(TextCommand.PermanentTextWithWrap));
            Assert.That(actual.TemporaryTextTime, Is.EqualTo(0));
            Assert.That(actual.Row, Is.EqualTo(1));
            Assert.That(actual.Column, Is.EqualTo(1));
            Assert.That(actual.Text, Is.EqualTo("************************"));
        }
    }
}
