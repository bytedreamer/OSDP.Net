using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages.ACU;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class DeviceCapabilitiesCommandTest
    {
        [TestCaseSource(typeof(DeviceCapabilitiesCommandTestClass), nameof(DeviceCapabilitiesCommandTestClass.TestCases))]
        public string DeviceCapabilitiesCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            var deviceCapabilitiesCommand = new DeviceCapabilitiesCommand(address);
            var device = new DeviceProxy(0, useCrc, useSecureChannel, null);
            device.MessageControl.IncrementSequence(1);
            return BitConverter.ToString(deviceCapabilitiesCommand.BuildCommand(device));
        }

        public class DeviceCapabilitiesCommandTestClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData((byte) 0x0, true, true).Returns(
                        "53-00-0B-00-0E-02-17-62-00-38-37");
                    yield return new TestCaseData((byte) 0x0, true, false).Returns(
                        "53-00-09-00-06-62-00-F3-5D");
                    yield return new TestCaseData((byte) 0x0, false, false).Returns(
                        "53-00-08-00-02-62-00-41");
                }
            }
        }
    }
}