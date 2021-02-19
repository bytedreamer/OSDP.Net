using System.Collections.Generic;
using NUnit.Framework;
using OSDP.Net.Model.ReplyData;

namespace OSDP.Net.Tests.Model.ReplyData
{
    [TestFixture]
    public class KeypadDataTest
    {
        [Test]
        public void ParseData()
        {
            // Arrange
            var data = new List<byte> {0x01, 0x05};

            data.AddRange(new byte[] {0x00, 0x01, 0x02, 0x03, 0x04});

            // Act
            var keypadData = KeypadData.ParseData(data.ToArray());

            // Assert
            Assert.AreEqual(1, keypadData.ReaderNumber);
            Assert.AreEqual(5, keypadData.DigitCount);
            Assert.AreEqual(new byte[] {0x00, 0x01, 0x02, 0x03, 0x04}, keypadData.Data);
        }

        [Test]
        public void ParseNoData()
        {
            // Arrange
            var data = new List<byte> {0x01, 0x00};

            data.AddRange(new byte[] {});

            // Act
            var keypadData = KeypadData.ParseData(data.ToArray());

            // Assert
            Assert.AreEqual(1, keypadData.ReaderNumber);
            Assert.AreEqual(0, keypadData.DigitCount);
            Assert.AreEqual(new byte[] {}, keypadData.Data);
        }
    }
}