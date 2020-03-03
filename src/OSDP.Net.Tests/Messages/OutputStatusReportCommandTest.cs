using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class OutputStatusReportCommandTest
    {
        [TestCaseSource(typeof(OutputStatusReportCommandTestClass), nameof(OutputStatusReportCommandTestClass.TestCases))]
        public string BuildCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            var outputStatusReportCommand = new OutputStatusReportCommand(address);
            return BitConverter.ToString(
                outputStatusReportCommand.BuildCommand(new Device(0, useCrc, useSecureChannel)));
        }

        private class OutputStatusReportCommandTestClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData((byte) 0x0, true, true).Returns(
                        "53-00-0A-00-0C-02-15-66-9E-B5");
                    yield return new TestCaseData((byte) 0x0, true, false).Returns(
                        "53-00-08-00-04-66-2D-CA");
                    yield return new TestCaseData((byte) 0x0, false, false).Returns(
                        "53-00-07-00-00-66-40");
                }
            }
        }
    }
}