using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages;

namespace OSDP.Net.Tests.Messages
{
    [TestFixture]
    public class PollCommandTest
    {
        [TestCaseSource(typeof(MyDataClass), nameof(MyDataClass.TestCases))]
        public string PollCommand_TestCases(byte address, Control control)
        {
            return BitConverter.ToString(new PollCommand().BuildCommand(address, control));
        }
    }
    
    public class MyDataClass
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData((byte) 0x0, new Control(0, true, false)).Returns(
                    "53-00-08-00-04-60-EB-AA");
            }
        }  
    }
}