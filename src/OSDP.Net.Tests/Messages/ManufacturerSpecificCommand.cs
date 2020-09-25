using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages;
using OSDP.Net.Model.CommandData;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class ManufacturerSpecificCommandTest
    {
        [TestCaseSource(typeof(ManufacturerSpecificCommandTestDataClass), nameof(ManufacturerSpecificCommandTestDataClass.TestCases))]
        public string BuildCommand_TestCases(byte address, bool useCrc, bool useSecureChannel, ManufacturerSpecificCommandData manufacturerSpecificCommandData)
        {
            var manufacturerSpecificCommand = new ManufacturerSpecificCommand(address, manufacturerSpecificCommandData);
            return BitConverter.ToString(manufacturerSpecificCommand.BuildCommand(new Device(address, useCrc, useSecureChannel)));
        }

        public class ManufacturerSpecificCommandTestDataClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    var mfgData = new ManufacturerSpecificCommandData(new byte[] { 0x01, 0x02, 0x03 }, new byte[] { 0x0A, 0x0B, 0x0C });

                    yield return new TestCaseData((byte) 0x0, true, true, mfgData).Returns(
                        "53-00-10-00-0C-02-17-80-01-02-03-0A-0B-0C-B5-0D");
                    yield return new TestCaseData((byte) 0x0, true, false, mfgData).Returns(
                        "53-00-0E-00-04-80-01-02-03-0A-0B-0C-CD-9B");
                    yield return new TestCaseData((byte) 0x0, false, false, mfgData).Returns(
                        "53-00-0D-00-00-80-01-02-03-0A-0B-0C-F9");
                }
            }
        }
    }
}