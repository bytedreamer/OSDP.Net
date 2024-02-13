using System;
using System.Collections;
using NUnit.Framework;
using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;
using OSDP.Net.Model.CommandData;
using OutgoingMessage = OSDP.Net.Messages.OutgoingMessage;

namespace OSDP.Net.Tests.Messages;

[TestFixture]
public class CommandOutgoingMessageTest
{
    [TestCaseSource(typeof(CommunicationConfigurationBuildMessageTestClass),
        nameof(CommunicationConfigurationBuildMessageTestClass.TestCases))]
    public string CommunicationConfigurationBuildMessage_TestCases(DeviceProxy device)
    {
        device.MessageControl.IncrementSequence(1);

        var outgoingMessage = new OutgoingMessage(device.Address, device.MessageControl,
            new CommunicationConfiguration(1, 9600));

        return BitConverter.ToString(
            outgoingMessage.BuildMessage(CreateSecureChannel(device.UseSecureChannel)));
    }

    public class CommunicationConfigurationBuildMessageTestClass
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData(new DeviceProxy(0x00, true, true)).Returns(
                    "FF-53-00-1E-00-0E-02-17-6E-AC-A3-C2-FF-E4-D9-E5-99-CD-77-1B-8E-F3-D1-3F-23-FA-AB-63-4E-08-95");
                yield return new TestCaseData(new DeviceProxy(0x00, true, false)).Returns(
                    "FF-53-00-0D-00-06-6E-01-80-25-00-00-EC-7B");
                yield return new TestCaseData(new DeviceProxy(0x00, false, false)).Returns(
                    "FF-53-00-0C-00-02-6E-01-80-25-00-00-8B");
            }
        }
    }

    [TestCaseSource(typeof(DeviceCapabilitiesBuildMessageTestClass),
        nameof(DeviceCapabilitiesBuildMessageTestClass.TestCases))]
    public string DeviceCapabilitiesBuildMessage_TestCases(DeviceProxy device)
    {
        device.MessageControl.IncrementSequence(1);

        var outgoingMessage = new OutgoingMessage(device.Address, device.MessageControl,
            new DeviceCapabilities());

        return BitConverter.ToString(
            outgoingMessage.BuildMessage(CreateSecureChannel(device.UseSecureChannel)));
    }

    public class DeviceCapabilitiesBuildMessageTestClass
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData(new DeviceProxy(0x00, true, true)).Returns(
                    "FF-53-00-1E-00-0E-02-17-62-D1-4E-B7-AD-98-2C-C5-F9-AA-25-31-8A-7E-AE-D6-26-0F-6A-2A-A7-38-70");
                yield return new TestCaseData(new DeviceProxy(0x00, true, false)).Returns(
                    "FF-53-00-09-00-06-62-00-F3-5D");
                yield return new TestCaseData(new DeviceProxy(0x00, false, false)).Returns(
                    "FF-53-00-08-00-02-62-00-41");
            }
        }
    }

    [TestCaseSource(typeof(PollBuildMessageTestClass), nameof(PollBuildMessageTestClass.TestCases))]
    public string PollBuildMessage_TestCases(DeviceProxy device)
    {
        device.MessageControl.IncrementSequence(1);

        var outgoingMessage = new OutgoingMessage(device.Address, device.MessageControl,
            new NoPayloadCommandData(CommandType.Poll));

        return BitConverter.ToString(
            outgoingMessage.BuildMessage(CreateSecureChannel(device.UseSecureChannel)));
    }


    public class PollBuildMessageTestClass
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData(new DeviceProxy(0x0, true, true)).Returns(
                    "FF-53-00-0E-00-0E-02-15-60-4B-32-76-77-73-B6");
                yield return new TestCaseData(new DeviceProxy(0x0, true, false)).Returns(
                    "FF-53-00-08-00-06-60-89-CC");
                yield return new TestCaseData(new DeviceProxy(0x0, false, false)).Returns(
                    "FF-53-00-07-00-02-60-44");
            }
        }
    }

    private ACUMessageSecureChannel CreateSecureChannel(bool useSecureChannel)
    {
        return new ACUMessageSecureChannel(new SecurityContext(SecurityContext.DefaultKey)
        {
            IsSecurityEstablished = useSecureChannel,
            Enc = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            RMac = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            SMac1 = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            SMac2 = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]
        });
    }
}