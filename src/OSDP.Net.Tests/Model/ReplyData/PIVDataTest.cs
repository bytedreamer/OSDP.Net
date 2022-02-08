using System.Collections.Generic;
using NUnit.Framework;
using OSDP.Net.Messages;
using OSDP.Net.Model.ReplyData;

namespace OSDP.Net.Tests.Model.ReplyData
{
    public class PIVDataTest
    {
        [Test]
        public void ParseData()
        {
            // Arrange
            var data = new List<byte>();

            data.AddRange(Message.ConvertShortToBytes(20));
            data.AddRange(Message.ConvertShortToBytes(10));
            data.AddRange(Message.ConvertShortToBytes(5));
            data.AddRange(new byte[] {0x00, 0x01, 0x02, 0x03, 0x04});

            // Act
            var pivData = DataFragmentResponse.ParseData(data.ToArray());

            // Assert
            Assert.AreEqual(20, pivData.WholeMessageLength);
            Assert.AreEqual(10, pivData.Offset);
            Assert.AreEqual(5, pivData.LengthOfFragment);
            Assert.AreEqual(new byte[] {0x00, 0x01, 0x02, 0x03, 0x04}, pivData.Data);
        }

        [Test]
        public void ParseDataNoData()
        {
            // Arrange
            var data = new List<byte>();

            data.AddRange(Message.ConvertShortToBytes(20));
            data.AddRange(Message.ConvertShortToBytes(10));
            data.AddRange(Message.ConvertShortToBytes(0));
            data.AddRange(new byte[] { });

            // Act
            var pivData = DataFragmentResponse.ParseData(data.ToArray());

            // Assert
            Assert.AreEqual(20, pivData.WholeMessageLength);
            Assert.AreEqual(10, pivData.Offset);
            Assert.AreEqual(0, pivData.LengthOfFragment);
            Assert.AreEqual(new byte[] { }, pivData.Data);
        }
    }
}