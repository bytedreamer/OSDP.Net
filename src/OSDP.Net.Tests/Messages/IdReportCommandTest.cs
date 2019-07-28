using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class IdReportCommandTest
    {
        [TestCaseSource(typeof(IdReportCommandDataClass), nameof(IdReportCommandDataClass.TestCases))]
        public string PollCommand_TestCases(byte address, Control control)
        {
            var idReportCommand = new IdReportCommand(address){ Control = control};
            return BitConverter.ToString(idReportCommand.BuildCommand());
        }

        private class IdReportCommandDataClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData((byte) 0x0, new Control(0, true, false)).Returns(
                        "53-00-09-00-04-61-00-C0-66");
                    yield return new TestCaseData((byte) 0x0, new Control(0, false, false)).Returns(
                        "53-00-08-00-00-61-00-44");
                }
            }
        }
    }
}