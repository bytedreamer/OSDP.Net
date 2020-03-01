using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class PollCommandTest
    {
        [TestCaseSource(typeof(PollCommandDataClass), nameof(PollCommandDataClass.TestCases))]
        public string PollCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            return BitConverter.ToString(new PollCommand(address).BuildCommand(new Device(0, useCrc, useSecureChannel)));
        }
    }

    public class PollCommandDataClass
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData((byte) 0x0, true, true).Returns(
                    "53-00-0A-00-0C-02-15-60-58-D5");
                yield return new TestCaseData((byte) 0x0, true, false).Returns(
                    "53-00-08-00-04-60-EB-AA");
                yield return new TestCaseData((byte) 0x0, false, false).Returns(
                    "53-00-07-00-00-60-46");
            }
        }
    }
}