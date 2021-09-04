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
            return BitConverter.ToString(getPIVDataCommand.BuildCommand(new Device(address, useCrc, useSecureChannel, null)));
        }

        public class GetPIVCommandTestDataClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    var data = new GetPIVData(ObjectId.CardholderUniqueIdentifier, 0, 0);

                    yield return new TestCaseData((byte) 0x0, true, true, data).Returns(
                        "53-00-0F-00-0C-02-17-A3-5F-C1-02-00-00-CB-A6");
                    yield return new TestCaseData((byte) 0x0, true, false, data).Returns(
                        "53-00-0D-00-04-A3-5F-C1-02-00-00-98-2A");
                    yield return new TestCaseData((byte) 0x0, false, false, data).Returns(
                        "53-00-0C-00-00-A3-5F-C1-02-00-00-DC");
                }
            }
        }
    }
}