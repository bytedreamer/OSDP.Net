using NUnit.Framework;
using System;
using System.Collections;
using System.Linq;
using OSDP.Net.Model.ReplyData;

namespace OSDP.Net.Tests.Model.ReplyData
{
    public class RawCardDataTest
    {
        [Test]
        public void ParseData()
        {
            var data = new byte[] { 0x05, 0x00, 0x10, 0x00, 0x12, 0xab };

            var rawCardData = RawCardData.ParseData(data);

            Assert.That(rawCardData.ReaderNumber, Is.EqualTo(5));
            Assert.That(rawCardData.FormatCode, Is.EqualTo(FormatCode.NotSpecified));
            Assert.That(rawCardData.BitCount, Is.EqualTo(16));
            Assert.That(RawCardData.FormatData(rawCardData.Data), Is.EqualTo("0001001010101011"));
        }

        [Test]
        public void BuildDataNoPadding()
        {
            // Resharper disable once ConditionIsAlwaysTrueOrFalse
            var data = new BitArray("0001001010101011".Select(
                x => x != '0' && (x == '1' ? true : throw new ArgumentException())).ToArray());

            var rawCardData = new RawCardData(5, FormatCode.NotSpecified, data);
            var buffer = rawCardData.BuildData();
            Assert.That(BitConverter.ToString(buffer), Is.EqualTo("05-00-10-00-12-AB"));
        }

        [TestCase("0001001010101011",
            ExpectedResult = "05-00-10-00-12-AB-80-00-00-00-00-00-00-00-00-00")]
        [TestCase("0001001010101011" + "1100110011001100" +
                  "1100110011001100" + "1100110011001100" +
                  "1100110011001100" + "11001100",
            ExpectedResult = "05-00-58-00-12-AB-CC-CC-CC-CC-CC-CC-CC-CC-CC-80")]
        [TestCase("0001001010101011" + "1100110011001100" +
                  "1100110011001100" + "1100110011001101" +
                  "1100110011001100" + "1100110011001100",
            ExpectedResult = "05-00-60-00-12-AB-CC-CC-CC-CC-CC-CD-CC-CC-CC-CC-" +
                             "80-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00")]
        public string BuildDataWithPadding(string keyData)
        {
            // Resharper disable once ConditionIsAlwaysTrueOrFalse
            var data = new BitArray(keyData.Select(
                x => x != '0' && (x == '1' ? true : throw new Exception("invalid input data"))).ToArray());

            var rawCardData = new RawCardData(5, FormatCode.NotSpecified, data);
            var buffer = rawCardData.BuildData(true);
            return BitConverter.ToString(buffer);
        }
    }
}
