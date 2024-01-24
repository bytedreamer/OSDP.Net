using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages.ACU;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class OutputStatusReportCommandTest
    {
        [TestCaseSource(typeof(OutputStatusReportCommandTestClass), nameof(OutputStatusReportCommandTestClass.TestCases))]
        public string BuildCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            var outputStatusReportCommand = new OutputStatusReportCommand(address);
            var device = new DeviceProxy(0, useCrc, useSecureChannel, null);
            device.MessageControl.IncrementSequence(1);
            return BitConverter.ToString(
                outputStatusReportCommand.BuildCommand(device));
        }

        public class OutputStatusReportCommandTestClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData((byte) 0x0, true, true).Returns(
                        "53-00-0A-00-0E-02-15-66-F6-58");
                    yield return new TestCaseData((byte) 0x0, true, false).Returns(
                        "53-00-08-00-06-66-4F-AC");
                    yield return new TestCaseData((byte) 0x0, false, false).Returns(
                        "53-00-07-00-02-66-3E");
                }
            }
        }
    }
}