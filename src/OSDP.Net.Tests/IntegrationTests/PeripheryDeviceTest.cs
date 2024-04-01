using System;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using NUnit.Framework;
using OSDP.Net.Connections;
using OSDP.Net.Model;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Model.ReplyData;
using DeviceCapabilities = OSDP.Net.Model.ReplyData.DeviceCapabilities;
using System.Linq;
using Moq;
using System.Collections.Concurrent;

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
    private const int DefaultTestBaud = 9600;
    private const byte DefaultTestDeviceAddr = 0;

    private ILoggerFactory _loggerFactory;
    private ControlPanel _targetPanel;
    private TestDevice _targetDevice;

    private byte _deviceAddress;
    private Guid _connectionId;

    private TaskCompletionSource<bool> _deviceOnlineCompletionSource;
    private ConcurrentQueue<EventCheckpoint> _eventCheckpoints = new();
    private object _syncLock = new object();


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
    public async Task Teardown() 
    {
        await _targetPanel?.Shutdown();
        await _targetDevice.StopListening();

        _targetDevice?.Dispose();
        _loggerFactory?.Dispose();
    }

    // TODO: This and next test can be parameterized
    [Test]
    public async Task TestEstablishingConnectionWithNonDefaultSecurityKey()
    {
        var securityKey = new byte[] { 0xa, 0xb, 0xc, 0xd, 0xe, 0xf, 0x1, 0x2, 0x3, 0x4, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf };

        await InitTestTargets(cfg => cfg.SecurityKey = securityKey);

        AddDeviceToPanel(securityKey);

        await WaitForDeviceOnlineStatus();
    }

    [Test]
    public async Task TestEstablishingConnectionWhenScBkIsSameAsDefaultKey()
    {
        var securityKey = "0123456789:;<=>?"u8.ToArray();

        await InitTestTargets(cfg => cfg.SecurityKey = securityKey);

        AddDeviceToPanel(securityKey);

        await WaitForDeviceOnlineStatus();
    }

    [Test]
    public async Task VerifyDefaultKeyIsRejectedWhenDefaultNotAllowed()
    {
        var securityKey = new byte[] { 0xa, 0xb, 0xc, 0xd, 0xe, 0xf, 0x1, 0x2, 0x3, 0x4, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf };

        // PD using non-default key and default key is NOT allowed
        await InitTestTargets(cfg => { cfg.SecurityKey = securityKey; cfg.DefaultSecurityKeyAllowed = false; });

        // Add device with a default key - this shouldn't connect
        AddDeviceToPanel();

        await AssertPanelRemainsDisconnected(10000);
    }

    [Test]
    public async Task VerifyDefaultKeyWorksWhenConfigAllowsItsUse()
    {
        var securityKey = new byte[] { 0xa, 0xb, 0xc, 0xd, 0xe, 0xf, 0x1, 0x2, 0x3, 0x4, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf };

        // PD using non-default key but ALSO allows default key
        await InitTestTargets(cfg => { cfg.SecurityKey = securityKey; cfg.DefaultSecurityKeyAllowed = true; });

        // Add device with a default key
        AddDeviceToPanel();

        await WaitForDeviceOnlineStatus();
    }

    [Test]
    public async Task VerifyDeviceWillIgnoreCommandsSentToDifferentAddress()
    {
        await InitTestTargets();

        AddDeviceToPanel(address: 5);

        await AssertPanelRemainsDisconnected();
    }

    [Test]
    public async Task KeySetCommandAbleToChangeSecureChannelKey()
    {
        await InitTestTargets();

        AddDeviceToPanel();

        await WaitForDeviceOnlineStatus();

        var newKey = new byte[] { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf };
        var result = await _targetPanel.EncryptionKeySet(_connectionId, 0, 
            new EncryptionKeyConfiguration(KeyType.SecureChannelBaseKey, newKey));
        Assert.True(result);

        await AssertPanelToDeviceCommsAreHealthy();

        RemoveDeviceFromPanel();
        AddDeviceToPanel(newKey);

        await WaitForDeviceOnlineStatus();
    }

    [Test]
    public async Task PanelIsAbleToChangeDeviceAddressWithComSetCommand()
    {
        await InitTestTargets();

        AddDeviceToPanel();

        await WaitForDeviceOnlineStatus();

        byte newAddress = 20;
        var commSettings = new Net.Model.CommandData.CommunicationConfiguration(newAddress, 9600);
        var results = await _targetPanel.CommunicationConfiguration(_connectionId, 0, commSettings);

        Assert.That(results.Address, Is.EqualTo(newAddress));

        Assert.ThrowsAsync<TimeoutException>(() => AssertPanelToDeviceCommsAreHealthy());

        RemoveDeviceFromPanel();
        AddDeviceToPanel(address:  newAddress);

        await WaitForDeviceOnlineStatus();

        await AssertPanelToDeviceCommsAreHealthy();
    }

    [Test]
    public async Task DeviceResetsItselfWhenPanelChangesBaudRateWithComSetCommand()
    {
        var mockComSetUpdate = new Mock<EventHandler<DeviceComSetUpdatedEventArgs>>();

        await InitTestTargets();

        _targetDevice.DeviceComSetUpdated += async (o, e) =>
        {
            TestContext.WriteLine("----- Received Device ComSet Updated EVENT -----");

            // Record call results so that we can verify them as part of the test
            mockComSetUpdate.Object.Invoke(o, e);

            // Simulate what a "real" client would do when it got the request to change comm settings
            await _targetDevice.StopListening();
            _targetDevice.Dispose();

            // Re-init the device with new baud rate
            InitTestTargetDevice(baudRate: e.NewBaudRate);
        };

        AddDeviceToPanel();

        await WaitForDeviceOnlineStatus();

        var connLostCheckpoint = SetupCheckpointForExpectedTestEvent(TestEventType.ConnectionLost);

        int newBaudRate = 19200;
        var commSettings = new Net.Model.CommandData.CommunicationConfiguration(_deviceAddress, newBaudRate);
        var results = await _targetPanel.CommunicationConfiguration(_connectionId, 0, commSettings);

        Assert.AreEqual(results.Address, _deviceAddress);
        Assert.AreEqual(results.BaudRate, newBaudRate);

        await connLostCheckpoint;

        mockComSetUpdate.Verify(e => e(
            It.IsAny<object>(),
            It.IsAny<DeviceComSetUpdatedEventArgs>()), Times.Once);

        var eventArgs = mockComSetUpdate.Invocations.First().Arguments[1] as DeviceComSetUpdatedEventArgs;
        Assert.Multiple(() =>
        {
            Assert.AreEqual(0, eventArgs.OldAddress);
            Assert.AreEqual(0, eventArgs.NewAddress);
            Assert.AreEqual(9600, eventArgs.OldBaudRate);
            Assert.AreEqual(19200, eventArgs.NewBaudRate);
        });

        RemoveDeviceFromPanel();
        await RestartTargetPanelConnection(baudRate: newBaudRate);
        
        AddDeviceToPanel();

        await WaitForDeviceOnlineStatus();
    }

    private async Task InitTestTargets(Action<DeviceConfiguration> configureDevice = null)
    {
        InitTestTargetDevice(configureDevice, baudRate: DefaultTestBaud);
        await InitTestTargetPanel(baudRate: DefaultTestBaud);
    }

    private async Task InitTestTargetPanel(int baudRate = DefaultTestBaud)
    {
        _deviceOnlineCompletionSource = new TaskCompletionSource<bool>();

        _targetPanel = new ControlPanel(_loggerFactory.CreateLogger<ControlPanel>());
        _targetPanel.ConnectionStatusChanged += (_, e) =>
        {
            TestContext.WriteLine($"Received event: {e}");

            if (e.ConnectionId != _connectionId) return;

            lock (_syncLock)
            {
                TestEventType currentEventType;

                if (e.IsConnected)
                {
                    _deviceOnlineCompletionSource.TrySetResult(true);
                    currentEventType = TestEventType.ConnectionEstablished;
                }
                else
                {
                    currentEventType = TestEventType.ConnectionLost;
                    if (_deviceOnlineCompletionSource.Task.IsCompleted)
                    {
                        _deviceOnlineCompletionSource = new TaskCompletionSource<bool>();
                    }
                }

                if (_eventCheckpoints.TryPeek(out var checkpoint) && checkpoint.EventType == currentEventType)
                {
                    _eventCheckpoints.TryDequeue(out var _);
                    checkpoint.Tcs.TrySetResult(true);
                }
            }
        };

        await RestartTargetPanelConnection(baudRate);
    }

    private async Task RestartTargetPanelConnection(int baudRate = DefaultTestBaud)
    {
        if (_connectionId != Guid.Empty)
        {
            await _targetPanel.StopConnection(_connectionId);
        }

        _connectionId = _targetPanel.StartConnection(new TcpClientOsdpConnection("localhost", 6000, baudRate));
    }

    private void InitTestTargetDevice(Action<DeviceConfiguration> configureDevice = null, int baudRate = DefaultTestBaud)
    {
        var deviceConfig = new DeviceConfiguration() { Address = DefaultTestDeviceAddr };
        configureDevice?.Invoke(deviceConfig);

        _deviceAddress = deviceConfig.Address;

        _targetDevice = new TestDevice(deviceConfig, _loggerFactory);
        _targetDevice.StartListening(new TcpOsdpServer(6000, baudRate, _loggerFactory));
    }

    private void AddDeviceToPanel(byte[] securityKey = null, byte? address = null)
    {
        if (address != null)
        {
            _deviceAddress = address.Value;
        }

        _deviceOnlineCompletionSource = new TaskCompletionSource<bool>();
        _targetPanel.AddDevice(_connectionId, _deviceAddress, true, true, securityKey);
    }

    private void RemoveDeviceFromPanel()
    {
        _targetPanel.RemoveDevice(_connectionId, _deviceAddress);
    }

    private async Task WaitForDeviceOnlineStatus(int timeout = 10000)
    {
        var onlineTask = _deviceOnlineCompletionSource.Task;
        if (await Task.WhenAny(onlineTask, Task.Delay(timeout)) != onlineTask)
        {
            Assert.Fail("Timeout waiting for device connection to come online");
        }
    }

    private async Task AssertPanelRemainsDisconnected(int timeout = 10000)
    {
        var onlineTask = _deviceOnlineCompletionSource.Task;
        if (await Task.WhenAny(onlineTask, Task.Delay(timeout)) == onlineTask)
        {
            Assert.Fail("This connections was expected to fail but IT DID NOT!!");
        }
    }

    private async Task AssertPanelToDeviceCommsAreHealthy()
    {
        var capabilities = await _targetPanel.DeviceCapabilities(_connectionId, _deviceAddress);
        Assert.NotNull(capabilities);
    }

    private async Task SetupCheckpointForExpectedTestEvent(TestEventType eventType, int timeout = 10000)
    {
        TaskCompletionSource<bool> tcs = new();

        _eventCheckpoints.Enqueue(new EventCheckpoint() { EventType = eventType, Tcs = tcs });

        var result = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
        if (result != tcs.Task)
        {
            Assert.Fail($"Timeout waiting for event checkpoint '{eventType}'");
        }
    }

    private enum TestEventType
    {
        ConnectionLost,
        ConnectionEstablished
    }

    private class EventCheckpoint
    {
        public TestEventType EventType { get; set; }
        
        public TaskCompletionSource<bool> Tcs { get; set; }
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

    protected override PayloadData HandleCommunicationSet(Net.Model.CommandData.CommunicationConfiguration commandPayload)
    {
        var validBaudRates = new int[] { 9600, 19200, 115200 };
        var newBaudRate = validBaudRates.Contains(commandPayload.BaudRate) ? commandPayload.BaudRate : validBaudRates[0];

        return new Net.Model.ReplyData.CommunicationConfiguration(commandPayload.Address, newBaudRate);
    }
}
