using NUnit.Framework;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Utilities;
using System;
using System.Linq;

namespace OSDP.Net.Tests.Model.CommandData
{
    internal class GetPIVDataTest
    {
        [Test]
        public void BuildData() 
        {
            var command = new GetPIVData(ObjectId.CardholderUniqueIdentifier, 1, 25);

            var actual = command.BuildData().ToArray();
            Assert.That(BitConverter.ToString(actual), Is.EqualTo("5F-C1-02-01-19"));
        }

        [Test]
        public void BuildData2()
        {
            var command = new GetPIVData(new byte[] { 0x5F, 0xC1, 0x02 }, 1, 25);

            var actual = command.BuildData().ToArray();
            Assert.That(BitConverter.ToString(actual), Is.EqualTo("5F-C1-02-01-19-00"));
        }

        [TestCase("5F-C1-02-01-19-00")]
        [TestCase("5F-C1-02-01-19")]
        public void ParseData(string inputData)
        {
            var actual = GetPIVData.ParseData(BinaryUtils.HexToBytes(inputData).ToArray());

            Assert.That(actual.ObjectId, Is.EquivalentTo(new byte[] { 0x5F, 0xC1, 0x02 }));
            Assert.That(actual.ElementId, Is.EqualTo(1));
            Assert.That(actual.DataOffset, Is.EqualTo(25));
        }
    }
}
