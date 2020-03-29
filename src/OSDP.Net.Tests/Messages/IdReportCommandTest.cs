using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class IdReportCommandTest
    {
        [TestCaseSource(typeof(IdReportCommandDataClass), nameof(IdReportCommandDataClass.TestCases))]
        public string BuildCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            var idReportCommand = new IdReportCommand(address);
            return BitConverter.ToString(idReportCommand.BuildCommand(new Device(0, useCrc, useSecureChannel)));
        }

        public class IdReportCommandDataClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData((byte) 0x0, true, true).Returns(
                        "53-00-0B-00-0C-02-17-61-00-E8-26");
                    yield return new TestCaseData((byte) 0x0, true, false).Returns(
                        "53-00-09-00-04-61-00-C0-66");
                    yield return new TestCaseData((byte) 0x0, false, false).Returns(
                        "53-00-08-00-00-61-00-44");
                }
            }
        }
    }
}