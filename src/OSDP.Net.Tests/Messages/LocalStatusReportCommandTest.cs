using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class LocalStatusReportCommandTest
    {
        [TestCaseSource(typeof(LocalStatusReportCommandTestClass), nameof(LocalStatusReportCommandTestClass.TestCases))]
        public string BuildCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            var localStatusReportCommand = new LocalStatusReportCommand(address);
            return BitConverter.ToString(
                localStatusReportCommand.BuildCommand(new Device(0, useCrc, useSecureChannel)));
        }

        public class LocalStatusReportCommandTestClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData((byte) 0x0, true, true).Returns(
                        "53-00-0A-00-0C-02-15-64-DC-95");
                    yield return new TestCaseData((byte) 0x0, true, false).Returns(
                        "53-00-08-00-04-64-6F-EA");
                    yield return new TestCaseData((byte) 0x0, false, false).Returns(
                        "53-00-07-00-00-64-42");
                }
            }
        }
    }
}