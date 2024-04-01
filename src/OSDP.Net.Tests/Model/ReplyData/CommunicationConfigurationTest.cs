using NUnit.Framework;
using OSDP.Net.Model.ReplyData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSDP.Net.Tests.Model.ReplyData
{
    [TestFixture]
    public class CommunicationConfigurationTest
    {
        [Test]
        public void ParseData()
        {
            var data = new byte[] { 0x19, 0x80, 0x25, 0x00, 0x00 };
            var comConfig = CommunicationConfiguration.ParseData(data);

            Assert.That(comConfig.Address, Is.EqualTo(25));
            Assert.That(comConfig.BaudRate, Is.EqualTo(9600));
        }

        [Test]
        public void BuildData()
        {
            var comConfig = new CommunicationConfiguration(25, 9600);
            var buffer = comConfig.BuildData();

            Assert.That(BitConverter.ToString(buffer), Is.EqualTo("19-80-25-00-00"));
        }
    }
}
