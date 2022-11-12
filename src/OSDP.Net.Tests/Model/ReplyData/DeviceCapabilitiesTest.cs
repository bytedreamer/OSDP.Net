using System;
using System.Linq;
using NUnit.Framework;
using OSDP.Net.Model.ReplyData;



namespace OSDP.Net.Tests.Model.ReplyData
{
    [TestFixture]
    public class DeviceCapabilitiesTest
    {
        private readonly byte[] _rawCapsFromDennisBrivoKeypad = new byte[] {
            3, 1, 1, 4, 4, 1, 5, 2, 1, 6, 0, 0, 7, 0, 0, 8, 1,
            0, 9, 1, 1, 10, 194, 1, 14, 0, 0, 15, 0, 0, 16, 1, 0
        };

        [Test]
        public void ThrowsWhenDataWrongLength()
        {
            var rawData = new byte[] { 0x01, 0x02 };

            Assert.Throws<Exception>(() => DeviceCapabilities.ParseData(rawData));
        }

        [Test]
        public void ParsesOutFunctionCodes()
        {
            #pragma warning disable CS0618 // Type or member is obsolete
            var expectedFuncCodes = new []
            {
                CapabilityFunction.CardDataFormat,
                CapabilityFunction.ReaderLEDControl,
                CapabilityFunction.ReaderAudibleOutput,
                CapabilityFunction.ReaderTextOutput,
                CapabilityFunction.TimeKeeping,
                CapabilityFunction.CheckCharacterSupport,
                CapabilityFunction.CommunicationSecurity,
                CapabilityFunction.ReceiveBufferSize,
                CapabilityFunction.Biometrics,
                CapabilityFunction.SecurePINEntry, 
                CapabilityFunction.OSDPVersion
            };
            #pragma warning restore CS0618 // Type or member is obsolete

            var actual = DeviceCapabilities.ParseData(_rawCapsFromDennisBrivoKeypad);

            Assert.That(actual.Capabilities.Select((x) => x.Function), Is.EquivalentTo(expectedFuncCodes));
        }

        [Test]
        public void CommSecurityCapInstanceFromDerivedClass()
        {
            var actual = DeviceCapabilities.ParseData(_rawCapsFromDennisBrivoKeypad);

            var commSecCap = actual.Get<CommSecurityDeviceCap>(CapabilityFunction.CommunicationSecurity);

            Assert.That(commSecCap.SupportsAes128, Is.EqualTo(true));
            Assert.That(commSecCap.UsesDefaultKey, Is.EqualTo(true));
        }

        [Test]
        public void ToStringTest()
        {
            var actual = DeviceCapabilities.ParseData(_rawCapsFromDennisBrivoKeypad.AsSpan().Slice(18, 9));
            var expectedText =
                "  Function: Communication Security\r\n" +
                "Supports AES-128: True\r\n" +
                "Uses Default Key: True\r\n" + 
                "\r\n" + 
                "  Function: Receive Buffer Size\r\n" +
                "      Size: 450\r\n" +
                "\r\n" +
                "  Function: Biometrics\r\n" +
                "Compliance: 0\r\n" +
                " Number Of: 0\r\n" +
                "\r\n";

            Assert.That(actual.ToString(), Is.EqualTo(expectedText).NoClip);
        }
    }
}
