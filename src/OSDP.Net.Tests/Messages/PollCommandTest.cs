using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages.ACU;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class PollCommandTest
    {
        [TestCaseSource(typeof(PollCommandDataClass), nameof(PollCommandDataClass.TestCases))]
        public string BuildCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            var device = new DeviceProxy(0, useCrc, useSecureChannel, null);
            device.MessageControl.IncrementSequence(1);
            return BitConverter.ToString(new PollCommand(address).BuildCommand(device));
        }
    }

    public class PollCommandDataClass
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData((byte) 0x0, true, true).Returns(
                    "53-00-0A-00-0E-02-15-60-30-38");
                yield return new TestCaseData((byte) 0x0, true, false).Returns(
                    "53-00-08-00-06-60-89-CC");
                yield return new TestCaseData((byte) 0x0, false, false).Returns(
                    "53-00-07-00-02-60-44");
            }
        }
    }
}