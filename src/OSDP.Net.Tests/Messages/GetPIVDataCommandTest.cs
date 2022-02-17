using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class GetPIVDataCommandTest
    {
        [TestCaseSource(typeof(GetPIVCommandTestDataClass), nameof(GetPIVCommandTestDataClass.TestCases))]
        public string BuildCommand_TestCases(byte address, bool useCrc, bool useSecureChannel, GetPIVData getPIVData)
        {
            var getPIVDataCommand = new GetPIVDataCommand(address, getPIVData);
            var device = new Device(0, useCrc, useSecureChannel, null);
            device.MessageControl.IncrementSequence(1);
            return BitConverter.ToString(getPIVDataCommand.BuildCommand(device));
        }

        public class GetPIVCommandTestDataClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    var data = new GetPIVData(new byte[]{0x5F, 0xC1, 0x02}, 0, 0);

                    yield return new TestCaseData((byte) 0x0, true, true, data).Returns(
                        "53-00-10-00-0E-02-17-A3-5F-C1-02-00-00-00-15-E4");
                    yield return new TestCaseData((byte) 0x0, true, false, data).Returns(
                        "53-00-0E-00-06-A3-5F-C1-02-00-00-00-41-23");
                    yield return new TestCaseData((byte) 0x0, false, false, data).Returns(
                        "53-00-0D-00-02-A3-5F-C1-02-00-00-00-D9");
                }
            }
        }
    }
}