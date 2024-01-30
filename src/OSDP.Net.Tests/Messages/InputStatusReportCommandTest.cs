using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages.ACU;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class InputStatusReportCommandTest
    {
        [TestCaseSource(typeof(InputStatusReportCommandTestClass), nameof(InputStatusReportCommandTestClass.TestCases))]
        public string BuildCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            var inputStatusReportCommand = new InputStatusReportCommand(address);
            var device = new DeviceProxy(0, useCrc, useSecureChannel, null);
            device.MessageControl.IncrementSequence(1);
            return BitConverter.ToString(
                inputStatusReportCommand.BuildCommand(device));
        }

        public class InputStatusReportCommandTestClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData((byte) 0x0, true, true).Returns(
                        "53-00-0A-00-0E-02-15-65-95-68");
                    yield return new TestCaseData((byte) 0x0, true, false).Returns(
                        "53-00-08-00-06-65-2C-9C");
                    yield return new TestCaseData((byte) 0x0, false, false).Returns(
                        "53-00-07-00-02-65-3F");
                }
            }
        }
    }
}