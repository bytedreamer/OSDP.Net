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
        public string PollCommand_TestCases(byte address, Control control)
        {
            return BitConverter.ToString(new PollCommand(address, control).BuildCommand());
        }
    }

    public class PollCommandDataClass
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData((byte) 0x0, new Control(0, true, false)).Returns(
                    "53-00-08-00-04-60-EB-AA");
                yield return new TestCaseData((byte) 0x0, new Control(0, false, false)).Returns(
                    "53-00-07-00-00-60-46");
            }
        }
    }
}