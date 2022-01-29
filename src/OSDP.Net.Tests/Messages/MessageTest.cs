using System;
using NUnit.Framework;
using OSDP.Net.Messages;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class MessageTest
    {
        [Test]
        public void BuildMultiPartMessageData()
        {
            // Arrange
            var part1 = new byte[] {0x00, 0x01};
            var part2 = new byte[] {0x02, 0x03, 0x04};
            var part3 = new byte[] {0x05};
            var completeData = new Span<byte>(new byte[6]);

            // Act
            Message.BuildMultiPartMessageData(6, 0, 2, part1, completeData);
            Message.BuildMultiPartMessageData(6, 2, 3, part2, completeData);
            Message.BuildMultiPartMessageData(6, 5, 1, part3, completeData);

            // Assert
            Assert.AreEqual(new byte[]{0x00, 0x01, 0x02, 0x03, 0x04, 0x05}, completeData.ToArray());
        }

        [Test]
        public void CalculateMaximumMessageSize_Clear()
        {
            // Arrange
            // Act
            ushort actual = Message.CalculateMaximumMessageSize(128);

            // Assert
            Assert.AreEqual(120, actual);
        }

        [Test]
        public void CalculateMaximumMessageSize_Encrypted()
        {
            // Arrange
            // Act
            ushort actual = Message.CalculateMaximumMessageSize(129, true);

            // Assert
            Assert.AreEqual(112, actual);
        }
    }
}