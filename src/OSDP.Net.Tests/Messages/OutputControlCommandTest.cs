using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class OutputControlCommandTest
    {
        [TestCaseSource(typeof(OutputControlCommandTestClass), nameof(OutputControlCommandTestClass.TestCases))]
        public string BuildCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            var outputControlCommand = new OutputControlCommand(address,
                new OutputControls(new[]
                {
                    new OutputControl(0, OutputControlCode.PermanentStateOffAllowTimedOperation, 100),
                    new OutputControl(1, OutputControlCode.TemporaryStateOffResumePermanentState, 120),
                }));
            return BitConverter.ToString(
                outputControlCommand.BuildCommand(new Device(0, useCrc, useSecureChannel)));
        }

        public class OutputControlCommandTestClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData((byte) 0x0, true, true).Returns(
                        "53-00-12-00-0C-02-17-68-00-03-64-00-01-06-78-00-57-19");
                    yield return new TestCaseData((byte) 0x0, true, false).Returns(
                        "53-00-10-00-04-68-00-03-64-00-01-06-78-00-81-85");
                    yield return new TestCaseData((byte) 0x0, false, false).Returns(
                        "53-00-0F-00-00-68-00-03-64-00-01-06-78-00-50");
                }
            }
        }
    }
}