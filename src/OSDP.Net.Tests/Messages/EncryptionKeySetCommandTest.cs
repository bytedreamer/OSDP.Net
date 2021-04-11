using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class EncryptionKeySetTestCommand
    {
        [TestCaseSource(typeof(EncryptionKeySetCommandTestClass), nameof(EncryptionKeySetCommandTestClass.TestCases))]
        public string BuildCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            var encryptionKeySetCommand = new EncryptionKeySetCommand(address,
                new EncryptionKeyConfiguration(KeyType.SecureChannelBaseKey,
                    new byte[]
                    {
                        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07
                    }));
            return BitConverter.ToString(encryptionKeySetCommand.BuildCommand(new Device(0, useCrc, useSecureChannel, null)));
        }

        public class EncryptionKeySetCommandTestClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData((byte) 0x0, true, true).Returns(
                        "53-00-1C-00-0C-02-17-75-01-10-00-01-02-03-04-05-06-07-00-01-02-03-04-05-06-07-48-29");
                    yield return new TestCaseData((byte) 0x0, true, false).Returns(
                        "53-00-1A-00-04-75-01-10-00-01-02-03-04-05-06-07-00-01-02-03-04-05-06-07-A9-C8");
                    yield return new TestCaseData((byte) 0x0, false, false).Returns(
                        "53-00-19-00-00-75-01-10-00-01-02-03-04-05-06-07-00-01-02-03-04-05-06-07-D6");
                }
            }
        }
    }
}