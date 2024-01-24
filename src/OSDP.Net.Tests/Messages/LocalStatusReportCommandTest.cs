using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages.ACU;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class LocalStatusReportCommandTest
    {
        [TestCaseSource(typeof(LocalStatusReportCommandTestClass), nameof(LocalStatusReportCommandTestClass.TestCases))]
        public string BuildCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            var localStatusReportCommand = new LocalStatusReportCommand(address);
            var device = new DeviceProxy(0, useCrc, useSecureChannel, null);
            device.MessageControl.IncrementSequence(1);
            return BitConverter.ToString(
                localStatusReportCommand.BuildCommand(device));
        }

        public class LocalStatusReportCommandTestClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData((byte) 0x0, true, true).Returns(
                        "53-00-0A-00-0E-02-15-64-B4-78");
                    yield return new TestCaseData((byte) 0x0, true, false).Returns(
                        "53-00-08-00-06-64-0D-8C");
                    yield return new TestCaseData((byte) 0x0, false, false).Returns(
                        "53-00-07-00-02-64-40");
                }
            }
        }
    }
}