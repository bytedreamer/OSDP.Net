using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class InputStatusReportCommandTest
    {
        [TestCaseSource(typeof(InputStatusReportCommandTestClass), nameof(InputStatusReportCommandTestClass.TestCases))]
        public string BuildCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            var inputStatusReportCommand = new InputStatusReportCommand(address);
            return BitConverter.ToString(
                inputStatusReportCommand.BuildCommand(new Device(0, useCrc, useSecureChannel)));
        }

        public class InputStatusReportCommandTestClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData((byte) 0x0, true, true).Returns(
                        "53-00-0A-00-0C-02-15-65-FD-85");
                    yield return new TestCaseData((byte) 0x0, true, false).Returns(
                        "53-00-08-00-04-65-4E-FA");
                    yield return new TestCaseData((byte) 0x0, false, false).Returns(
                        "53-00-07-00-00-65-41");
                }
            }
        }
    }
}