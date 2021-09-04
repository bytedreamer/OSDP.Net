using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class FileTransferCommandTest
    {
        [TestCaseSource(typeof(FileTransferCommandTestClass), nameof(FileTransferCommandTestClass.TestCases))]
        public string FileTransferCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            var fileTransferCommand = new FileTransferCommand(address,
                new FileTransfer(3, 1000, 10, 5, new byte[] {0x01, 0x02, 0x03, 0x04, 0x05}));
            return BitConverter.ToString(
                fileTransferCommand.BuildCommand(new Device(0, useCrc, useSecureChannel, null)));
        }

        public class FileTransferCommandTestClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData((byte) 0x0, true, true).Returns(
                        "53-00-1A-00-0C-02-17-7C-03-E8-03-00-00-0A-00-00-00-05-00-01-02-03-04-05-C9-03");
                    yield return new TestCaseData((byte) 0x0, true, false).Returns(
                        "53-00-18-00-04-7C-03-E8-03-00-00-0A-00-00-00-05-00-01-02-03-04-05-1A-D1");
                    yield return new TestCaseData((byte) 0x0, false, false).Returns(
                        "53-00-17-00-00-7C-03-E8-03-00-00-0A-00-00-00-05-00-01-02-03-04-05-0E");
                }
            }
        }
    }
}