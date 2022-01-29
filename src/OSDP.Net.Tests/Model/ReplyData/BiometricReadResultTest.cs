using System.Collections.Generic;
using NUnit.Framework;
using OSDP.Net.Messages;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Model.ReplyData;

namespace OSDP.Net.Tests.Model.ReplyData;

public class BiometricReadResultTest
{
    [Test]
    public void ParseData()
    {
        // Arrange
        var data = new List<byte> { 0x00, 0x00, 0x01, 0x50 };

        data.AddRange(Message.ConvertShortToBytes(5));
        data.AddRange(new byte[] {0x00, 0x01, 0x02, 0x03, 0x04});

        // Act
        var biometricReadResults = BiometricReadResult.ParseData(data.ToArray());

        // Assert
        Assert.AreEqual(0, biometricReadResults.ReaderNumber);
        Assert.AreEqual(BiometricStatus.Success, biometricReadResults.Status);
        Assert.AreEqual(BiometricType.RightThumbPrint, biometricReadResults.Type);
        Assert.AreEqual(0x50, biometricReadResults.Quality);
        Assert.AreEqual(5, biometricReadResults.Length);
        Assert.AreEqual(new byte[] {0x00, 0x01, 0x02, 0x03, 0x04}, biometricReadResults.TemplateData);
    }
}