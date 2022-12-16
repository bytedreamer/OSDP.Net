using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages.ACU;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class IdReportCommandTest
    {
        [TestCaseSource(typeof(IdReportCommandDataClass), nameof(IdReportCommandDataClass.TestCases))]
        public string BuildCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            var idReportCommand = new IdReportCommand(address);
            var device = new Device(0, useCrc, useSecureChannel, null);
            device.MessageControl.IncrementSequence(1);
            return BitConverter.ToString(idReportCommand.BuildCommand(device));
        }

        public class IdReportCommandDataClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData((byte) 0x0, true, true).Returns(
                        "53-00-0B-00-0E-02-17-61-00-6B-62");
                    yield return new TestCaseData((byte) 0x0, true, false).Returns(
                        "53-00-09-00-06-61-00-A0-08");
                    yield return new TestCaseData((byte) 0x0, false, false).Returns(
                        "53-00-08-00-02-61-00-42");
                }
            }
        }
    }
}