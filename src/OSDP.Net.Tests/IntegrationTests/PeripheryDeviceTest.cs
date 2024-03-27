using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using NUnit.Framework;
using OSDP.Net.Connections;
using OSDP.Net.Model;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Model.ReplyData;
using DeviceCapabilities = OSDP.Net.Model.ReplyData.DeviceCapabilities;

namespace OSDP.Net.Tests.IntegrationTests;


//
// NOTE: Majority of naming/structure in this file is very much a work-in-progress
// and will be updated if we continue to build out a set of integration tests
//
// Presently this is a POC experiment to see how far we can take a test harness that
// instantiates the ControlPanel (ACU) and a Device (PD) and establishes a set of
// full end-to-end test scenarios between those components.
//
// If/when this continues to evolve, we'll need refactor the tests into a set of helpers
// such that device setup/teardown code isn't repeated. And then a helper or two will 
// need to be added to make it easy/clear to write assertions which wait for certain
// events to occur (e.g. device came online).
//
// NOTE: Integration tests by nature are SLOWER than unit tests. Hence why they are 
// tagged with "Integration" category as we might want to exclude them at some point if
// the default PR test checks become too slow. There's only 5 tests here and they already
// take 25 sec to run. Then again, this might also highlight improvement opportunity
// in...
//   - ControlPanel and/or Device connection establishment logic. 
//   - ControlPanel's ability to signal when connection failed (right now we know when
//     it succeeds but if it doesn't we just keep trying until 10 sec timeout).
//



[TestFixture]
[Category("Integration")]
public class PeripheryDeviceTest
{
    private ILoggerFactory _loggerFactory;

    [SetUp]
    public void Setup()
    {
        // Each test gets spun up with its own console capture so we have to create
        // a new logger factory instance for every single test. Otherwise, the test runner
        // isn't able to associate stdout output with the particular test
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("OSDP.Net", LogLevel.Debug)
                .SetMinimumLevel(LogLevel.Debug)
                .AddConsole();
        });
    }

    [TearDown]
    public void Teardown() 
    {
        _loggerFactory?.Dispose();
    }

    [Test]
    public async Task TestEstablishingConnectionWithNonDefaultSecurityKey()
    {
        var securityKey = new byte[] { 0xa, 0xb, 0xc, 0xd, 0xe, 0xf, 0x1, 0x2, 0x3, 0x4, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf };
        var deviceConfig = new DeviceConfiguration() { SecurityKey = securityKey };
        using var device = new TestDevice(deviceConfig, _loggerFactory);
        var panel = new ControlPanel(_loggerFactory.CreateLogger<ControlPanel>());
        var tcsDeviceOnline = new TaskCompletionSource<bool>();

        try
        {
            device.StartListening(new TcpOsdpServer(6000, 9600, _loggerFactory));

            var connectionId = panel.StartConnection(new TcpClientOsdpConnection("localhost", 6000, 9600));

            panel.ConnectionStatusChanged += (sender, e) =>
            {
                TestContext.WriteLine($"Received event: {e}");

                if (e.ConnectionId == connectionId && e.IsConnected)
                {
                    tcsDeviceOnline.TrySetResult(true);
                }
            };

            panel.AddDevice(connectionId, 0, true, true, securityKey);

            if (await Task.WhenAny(tcsDeviceOnline.Task, Task.Delay(10000)) != tcsDeviceOnline.Task)
            {
                Assert.Fail("Timeout waiting for connection to come online");
            }
        }
        finally
        {
            await panel.Shutdown();
            await device.StopListening();
        }
    }

    [Test]
    public async Task TestEstablishingConnectionWhenScBkIsSameAsDefaultKey()
    {
        var securityKey = "0123456789:;<=>?"u8.ToArray();
        var deviceConfig = new DeviceConfiguration() { SecurityKey = securityKey };
        using var device = new TestDevice(deviceConfig, _loggerFactory);
        var panel = new ControlPanel(_loggerFactory.CreateLogger<ControlPanel>());
        var tcsDeviceOnline = new TaskCompletionSource<bool>();

        try
        {
            device.StartListening(new TcpOsdpServer(6000, 9600, _loggerFactory));

            var connectionId = panel.StartConnection(new TcpClientOsdpConnection("localhost", 6000, 9600));

            panel.ConnectionStatusChanged += (sender, e) =>
            {
                TestContext.WriteLine($"Received event: {e}");

                if (e.ConnectionId == connectionId && e.IsConnected)
                {
                    tcsDeviceOnline.TrySetResult(true);
                }
            };

            panel.AddDevice(connectionId, 0, true, true, securityKey);

            if (await Task.WhenAny(tcsDeviceOnline.Task, Task.Delay(10000)) != tcsDeviceOnline.Task)
            {
                Assert.Fail("Timeout waiting for connection to come online");
            }
        }
        finally
        {
            await panel.Shutdown();
            await device.StopListening();
        }
    }

    [Test]
    public async Task VerifyDefaultKeyIsRejectedWhenDefaultNotAllowed()
    {
        // PD using non-default key
        var securityKey = new byte[] { 0xa, 0xb, 0xc, 0xd, 0xe, 0xf, 0x1, 0x2, 0x3, 0x4, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf };
        var deviceConfig = new DeviceConfiguration() { SecurityKey = securityKey, DefaultSecurityKeyAllowed = false };
        using var device = new TestDevice(deviceConfig, _loggerFactory);

        var panel = new ControlPanel(_loggerFactory.CreateLogger<ControlPanel>());
        var tcsDeviceOnline = new TaskCompletionSource<bool>();

        try
        {
            device.StartListening(new TcpOsdpServer(6000, 9600, _loggerFactory));

            var connectionId = panel.StartConnection(new TcpClientOsdpConnection("localhost", 6000, 9600));

            panel.ConnectionStatusChanged += (sender, e) =>
            {
                TestContext.WriteLine($"Received event: {e}");

                if (e.ConnectionId == connectionId && e.IsConnected)
                {
                    tcsDeviceOnline.TrySetResult(true);
                }
            };

            // Add device with a default key - this shouldn't connect
            panel.AddDevice(connectionId, 0, true, true);

            if (await Task.WhenAny(tcsDeviceOnline.Task, Task.Delay(10000)) == tcsDeviceOnline.Task)
            {
                Assert.Fail("This connections was expected to fail but IT DID NOT!!");
            }
        }
        finally
        {
            await panel.Shutdown();
            await device.StopListening();
        }
    }

    [Test]
    public async Task VerifyDefaultKeyWorksWhenConfigAllowsItsUse()
    {
        // PD using non-default key but ALSO allows default key
        var securityKey = new byte[] { 0xa, 0xb, 0xc, 0xd, 0xe, 0xf, 0x1, 0x2, 0x3, 0x4, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf };
        var deviceConfig = new DeviceConfiguration() { SecurityKey = securityKey, DefaultSecurityKeyAllowed = true };
        using var device = new TestDevice(deviceConfig, _loggerFactory);

        var panel = new ControlPanel(_loggerFactory.CreateLogger<ControlPanel>());
        var tcsDeviceOnline = new TaskCompletionSource<bool>();

        try
        {
            device.StartListening(new TcpOsdpServer(6000, 9600, _loggerFactory));

            var connectionId = panel.StartConnection(new TcpClientOsdpConnection("localhost", 6000, 9600));

            panel.ConnectionStatusChanged += (sender, e) =>
            {
                TestContext.WriteLine($"Received event: {e}");

                if (e.ConnectionId == connectionId && e.IsConnected)
                {
                    tcsDeviceOnline.TrySetResult(true);
                }
            };

            // Add device with a default key
            panel.AddDevice(connectionId, 0, true, true);

            if (await Task.WhenAny(tcsDeviceOnline.Task, Task.Delay(10000)) != tcsDeviceOnline.Task)
            {
                Assert.Fail("Timeout waiting for connection to come online");
            }
        }
        finally
        {
            await panel.Shutdown();
            await device.StopListening();
        }
    }


    [Test]
    public async Task DeviceHandlesOsdpKeySetCommand()
    {
        var deviceConfig = new DeviceConfiguration();
        using var device = new TestDevice(deviceConfig, _loggerFactory);
        var panel = new ControlPanel(_loggerFactory.CreateLogger<ControlPanel>());
        var tcsDeviceOnline = new TaskCompletionSource<bool>();

        try
        {
            device.StartListening(new TcpOsdpServer(6000, 9600, _loggerFactory));

            var connectionId = panel.StartConnection(new TcpClientOsdpConnection("localhost", 6000, 9600));

            panel.ConnectionStatusChanged += (sender, e) =>
            {
                TestContext.WriteLine($"Received event: {e}");

                if (e.ConnectionId == connectionId && e.IsConnected)
                {
                    tcsDeviceOnline.TrySetResult(true);
                }
            };

            panel.AddDevice(connectionId, 0, true, true);

            // In my tests it takes solid 2 sec for connection to get signalled ONLINE. Therefore,
            // with some margin of safety we are using a 10sec timeout here to make sure tests don't 
            // get permanently stuck but also have enough time for connection magic to happen. 
            // 2 sec does seem awfully slow though
            if (await Task.WhenAny(tcsDeviceOnline.Task, Task.Delay(10000)) != tcsDeviceOnline.Task)
            {
                Assert.Fail("Timeout waiting for connection to come online");
            }

            var newKey = new byte[] { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf };
            var result = await panel.EncryptionKeySet(connectionId, 0, 
                new EncryptionKeyConfiguration(KeyType.SecureChannelBaseKey, newKey));
            Assert.True(result);

            await AssertPanelToDeviceCommsAreHealthy(panel, connectionId);

            panel.RemoveDevice(connectionId, 0);
            tcsDeviceOnline = new TaskCompletionSource<bool>();
            panel.AddDevice(connectionId, 0, true, true, newKey);

            if (await Task.WhenAny(tcsDeviceOnline.Task, Task.Delay(10000)) != tcsDeviceOnline.Task)
            {
                Assert.Fail("Timeout waiting for connection to come online");
            }
        }
        finally
        {
            await panel.Shutdown();
            await device.StopListening();
        }
    }

    private async Task AssertPanelToDeviceCommsAreHealthy(ControlPanel panel, Guid connectionId)
    {
        var capabilities = await panel.DeviceCapabilities(connectionId, 0);
        Assert.NotNull(capabilities);
    }
}


internal class TestDevice : Device
{
    public TestDevice(DeviceConfiguration config, ILoggerFactory loggerFactory)
        : base(config, loggerFactory) { }

    protected override PayloadData HandleIdReport()
    {
        return new DeviceIdentification([0x01, 0x02, 0x03], 4, 5, 6, 7, 8, 9);
    }

    protected override PayloadData HandleDeviceCapabilities()
    {
        var deviceCapabilities = new DeviceCapabilities(new[]
        {
            new DeviceCapability(CapabilityFunction.CardDataFormat, 1, 0),
            new DeviceCapability(CapabilityFunction.ReaderLEDControl, 1, 0),
            new DeviceCapability(CapabilityFunction.ReaderTextOutput, 0, 0),
            new DeviceCapability(CapabilityFunction.CheckCharacterSupport, 1, 0),
            new DeviceCapability(CapabilityFunction.CommunicationSecurity, 1, 1),
            new DeviceCapability(CapabilityFunction.ReceiveBufferSize, 0, 1),
            new DeviceCapability(CapabilityFunction.OSDPVersion, 2, 0)
        });

        return deviceCapabilities;
    }

    protected override PayloadData HandleKeySettings(EncryptionKeyConfiguration commandPayload)
    {
        return new Ack();
    }
}
