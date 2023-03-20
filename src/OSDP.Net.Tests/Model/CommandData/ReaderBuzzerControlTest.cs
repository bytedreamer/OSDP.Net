using NUnit.Framework;
using OSDP.Net.Utilities;
using System.Linq;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Tests.Model.CommandData
{
    internal class ReaderBuzzerControlTest
    {
        [Test]
        public void ParseData()
        {
            var inputData = BinaryUtils.HexToBytes("00-02-05-02-01").ToArray();

            var actual = ReaderBuzzerControl.ParseData(inputData);

            Assert.That(actual.ReaderNumber, Is.EqualTo(0));
            Assert.That(actual.ToneCode, Is.EqualTo(ToneCode.Default));
            Assert.That(actual.OnTime, Is.EqualTo(5));
            Assert.That(actual.OffTime, Is.EqualTo(2));
            Assert.That(actual.Count, Is.EqualTo(1));
        }
    }
}
 