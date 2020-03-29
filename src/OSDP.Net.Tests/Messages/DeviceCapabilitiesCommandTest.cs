using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class DeviceCapabilitiesCommandTest
    {
        [TestCaseSource(typeof(DeviceCapabilitiesCommandTestClass), nameof(DeviceCapabilitiesCommandTestClass.TestCases))]
        public string DeviceCapabilitiesCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            var deviceCapabilitiesCommand = new DeviceCapabilitiesCommand(address);
            return BitConverter.ToString(deviceCapabilitiesCommand.BuildCommand(new Device(0, useCrc, useSecureChannel)));
        }

        public class DeviceCapabilitiesCommandTestClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData((byte) 0x0, true, true).Returns(
                        "53-00-0B-00-0C-02-17-62-00-BB-73");
                    yield return new TestCaseData((byte) 0x0, true, false).Returns(
                        "53-00-09-00-04-62-00-93-33");
                    yield return new TestCaseData((byte) 0x0, false, false).Returns(
                        "53-00-08-00-00-62-00-43");
                }
            }
        }
    }
}