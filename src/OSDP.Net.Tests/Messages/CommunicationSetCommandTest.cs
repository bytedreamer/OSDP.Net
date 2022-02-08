using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class CommunicationSetCommandTest
    {
        [TestCaseSource(typeof(CommunicationSetCommandTestClass), nameof(CommunicationSetCommandTestClass.TestCases))]
        public string BuildCommand_TestCases(byte address, bool useCrc, bool useSecureChannel)
        {
            var communicationSetCommand = new CommunicationSetCommand(address, 
                new CommunicationConfiguration(1, 9600));
            var device = new Device(0, useCrc, useSecureChannel, null);
            device.MessageControl.IncrementSequence(1);
            return BitConverter.ToString(communicationSetCommand.BuildCommand(device));
        }

        public class CommunicationSetCommandTestClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData((byte) 0x0, true, true).Returns(
                        "53-00-0F-00-0E-02-17-6E-01-80-25-00-00-3B-51");
                    yield return new TestCaseData((byte) 0x0, true, false).Returns(
                        "53-00-0D-00-06-6E-01-80-25-00-00-EC-7B");
                    yield return new TestCaseData((byte) 0x0, false, false).Returns(
                        "53-00-0C-00-02-6E-01-80-25-00-00-8B");
                }
            }
        }
    }
}