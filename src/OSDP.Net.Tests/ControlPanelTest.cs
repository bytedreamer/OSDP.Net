using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OSDP.Net.Connections;
using OSDP.Net.Messages;
using OSDP.Net.Model.ReplyData;
using OSDP.Net.Tests.Utilities;

namespace OSDP.Net.Tests
{
    [TestFixture]
    public class ControlPanelTest
    {
        [Test]
        public async Task DeviceGoesOnlineTest()
        {
            // Arrange
            var connection = new TestConnection();

            var panel = new ControlPanel();
            Guid id = panel.StartConnection(connection);
            panel.AddDevice(id, 0, true, false);

            // Act
            // Assert
            await TaskEx.WaitUntil(() => panel.IsOnline(id, 0), TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(1));
        }

        [Test]
        public async Task ShutdownTest()
        {
            // Arrange
            var connection = new TestConnection();
            var panel = new ControlPanel();
            Guid id = panel.StartConnection(connection);
            panel.AddDevice(id, 0, true, false);

            // Act
            await panel.Shutdown();

            // Assert
            await TaskEx.WaitUntil(() => connection.NumberOfTimesCalledOpen == 1, TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(3));
            await TaskEx.WaitUntil(() => connection.NumberOfTimesCalledClose == 1, TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(3));
        }

        [Test]
        public async Task StartConnectionTest()
        {
            // Arrange
            var connection = new TestConnection();
            var panel = new ControlPanel();

            // Act
            Guid id = panel.StartConnection(connection);
            panel.AddDevice(id, 0, true, false);

            // Assert
            await TaskEx.WaitUntil(() => connection.NumberOfTimesCalledOpen == 1, TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(3));
        }

        [Test]
        public void StartConnectionWithSameConnectionTwiceTest()
        {
            // Arrange
            var connection = new TestConnection();
            var panel = new ControlPanel();
            var id = panel.StartConnection(connection);

            // Act/Assert
            var ex = Assert.Throws<InvalidOperationException>(() => panel.StartConnection(connection), "");
            Assert.That(ex?.Message, Is.EqualTo(
                $"The IOsdpConnection is already active in connection {id}. " +
                    "That connection must be stopped before starting a new one."));
        }

        [Test]
        public void StartSameConnectionConcurrentlyShouldOnlyStartItOnce()
        {
            // Arrange
            var connection = new TestConnection();
            var panel = new ControlPanel();

            // Act
            var tasks = Enumerable
                .Range(0, 100)
                .Select(_ => Task.Run(() => panel.StartConnection(connection)))
                .ToArray();
            try
            {
                // ReSharper disable once CoVariantArrayConversion
                Task.WaitAll(tasks);
            }
            catch { /* We handle errors later */ }

            // Assert
            Assert.That(tasks.Where(t => t.Status == TaskStatus.RanToCompletion).Count(), Is.EqualTo(1));
            Assert.That(tasks.Where(t => t.Status == TaskStatus.Faulted).Count(), Is.EqualTo(99));
        }

        [Test]
        public void StopSameConnectionConcurrentlyShouldSucceed()
        {
            // Arrange
            var connection = new TestConnection();
            var panel = new ControlPanel();
            var id = panel.StartConnection(connection);

            // Act
            var tasks = Enumerable
                .Range(0, 100)
                .Select(_ => Task.Run(async () => await panel.StopConnection(id)))
                .ToArray();
            try
            {
                // ReSharper disable once CoVariantArrayConversion
                Task.WaitAll(tasks);
            }
            catch { /* We handle errors later */ }

            // Assert
            Assert.That(tasks.Where(t => t.Status == TaskStatus.RanToCompletion).Count(), Is.EqualTo(100));
        }

        [Test]
        public async Task StartConnectionThatIsClosingShouldFail()
        {
            // Arrange
            var connection = new TestConnection();
            var openReachedEvent = new AutoResetEvent(false);
            var openCompleteEvent = new AutoResetEvent(false);
            connection.OpenAction = () => { openReachedEvent.Set(); openCompleteEvent.WaitOne(); };
            var panel = new ControlPanel();
            var id1 = panel.StartConnection(connection);

            // Act - Wait for the poll thread to reach open
            openReachedEvent.WaitOne();
            // Act - Initiate stop
            var stopTask = panel.StopConnection(id1);

            // Assert - Trying to start the connection while it's stopping fails
            Assert.Throws<InvalidOperationException>(() => panel.StartConnection(connection));

            // Act - Wait for the close to complete
            openCompleteEvent.Set();
            await stopTask;

            // Act - Starting again once the close is finished is ok.
            var id2 = panel.StartConnection(connection);

            // Assert - A new connection was created
            Assert.That(id1, Is.Not.EqualTo(id2));

            // Finish up.
            openCompleteEvent.Set();
            await panel.StopConnection(id2);
        }



        [Test]
        public async Task StartConnectionRestartWithSameConnectionTest()
        {
            // Arrange
            var connection = new TestConnection();
            var panel = new ControlPanel();

            // Act
            var id1 = panel.StartConnection(connection);
            await panel.StopConnection(id1);
            var id2 = panel.StartConnection(connection);
            await panel.StopConnection(id2);

            // Assert
            Assert.That(id1, Is.Not.EqualTo(id2));
        }


        [Test]
        public async Task StopConnectionTest()
        {
            // Arrange
            var connection = new TestConnection();
            var panel = new ControlPanel();

            Guid id = panel.StartConnection(connection);
            panel.AddDevice(id, 0, true, false);

            // Act
            await panel.StopConnection(id);

            // Assert
            await TaskEx.WaitUntil(() => connection.NumberOfTimesCalledOpen == 1, TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(3));
            await TaskEx.WaitUntil(() => connection.NumberOfTimesCalledClose == 1, TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(3));
        }

        [TestFixture]
        public class IdRequestCommandTest
        {
            [Test]
            public async Task ReturnsValidReportTest()
            {
                var panel = new ControlPanel(GlobalSetup.CreateLogger<ControlPanel>());
                var idReportCommandBytes = new byte[] { 255, 83, 0, 9, 0, 6, 97, 0, 160, 8 };
                var idReportReplyBytes = new byte[] { 83, 128, 20, 0, 6, 69, 92, 38, 35, 25, 2, 0, 0, 233, 42, 3, 0, 0, 98, 25 };

                var mockConnection = new MockConnection();
                mockConnection.OnCommand(idReportCommandBytes).Reply(idReportReplyBytes);

                Guid id = panel.StartConnection(mockConnection.Object);
                panel.AddDevice(id, 0, true, false);

                var response = await panel.IdReport(id, 0);

                Assert.That(response.ToString(), Is.EqualTo(
                    "     Vendor Code: 5C-26-23\r\n" +
                    "    Model Number: 25\r\n" +
                    "         Version: 2\r\n" +
                    "   Serial Number: 00-00-E9-2A\r\n" +
                    "Firmware Version: 3.0.0\r\n"
                ));
            }

            [Test]
            public void ThrowOnNakReplyTest()
            {
                var panel = new ControlPanel(GlobalSetup.CreateLogger<ControlPanel>());
                var idReportCommandBytes = new byte[] { 255, 83, 0, 9, 0, 6, 97, 0, 160, 8 };
                var nakReplyBytes = new byte[] { 83, 128, 9, 0, 7, 65, 3, 53, 221 };

                var mockConnection = new MockConnection();
                mockConnection.OnCommand(idReportCommandBytes).Reply(nakReplyBytes);

                Guid id = panel.StartConnection(mockConnection.Object);
                panel.AddDevice(id, 0, true, false);

                var exception = Assert.ThrowsAsync<NackReplyException>(async () => await panel.IdReport(id, 0));

                Assert.That(exception?.Reply.ErrorCode, Is.EqualTo(ErrorCode.UnknownCommandCode));
            }

        }

        class TestConnection : IOsdpConnection
        {
            private readonly MemoryStream _stream = new MemoryStream();

            public int NumberOfTimesCalledClose { get; private set; }

            public int NumberOfTimesCalledOpen { get; private set; }

            public int BaudRate => 9600;

            public bool IsOpen { get; private set; }

            public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromSeconds(1);

            public void Open()
            {
                NumberOfTimesCalledOpen++;
                IsOpen = true;
                OpenAction();
            }

            public Action OpenAction { get; set; } = () => { };

            public void Close()
            {
                NumberOfTimesCalledClose++;
                IsOpen = false;
            }

            public async Task WriteAsync(byte[] buffer)
            {
                await _stream.FlushAsync();

                await _stream.WriteAsync(
                    new AckReply().BuildReply(0, 
                        new Control((byte) (buffer[4] & 0x03), true, false)));

                _stream.Position = 0;
            }

            public async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
            {
                return await _stream.ReadAsync(buffer, 0, buffer.Length, token);
            }
        }

        class MockConnection : Mock<IOsdpConnection>
        {
            static readonly byte[] PollCommandBytes = new byte[] { 255, 83, 0, 8, 0, 4, 96, 235, 170 };
            static readonly byte[] AckReplyBytes = new byte[] { 83, 128, 8, 0, 5, 64, 104, 159 };

            public MockConnection() : base(MockBehavior.Strict)
            {
                var readStream = _incomingData.Reader.AsStream(true);

                Setup(x => x.IsOpen).Returns(true);
                Setup(x => x.BaudRate).Returns(9600);
                Setup(x => x.ReplyTimeout).Returns(TimeSpan.FromSeconds(999999));
                Setup(x => x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .Returns(async (byte[] buffer, CancellationToken cancellationToken) => 
                        await readStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                    );

                // Setup handling for a polling command which always gets issued when connection is alive
                // Here we'll just reply with a generic ACK to signal to ACU that the command was successfully
                // handled.
                OnCommand(PollCommandBytes).Reply(AckReplyBytes);
            }

            public ExpectedCommand OnCommand(byte[] commandBytes) => new(this, commandBytes);

            public class ExpectedCommand
            {
                public ExpectedCommand(MockConnection parent, byte[] commandBytes)
                {
                    _parent = parent;
                    _commandBytes = commandBytes;
                }

                public void Reply(byte[] replyBytes)
                {
                    for (byte seq = 0; seq < 4; seq++)
                    {
                        // Make local copies because we'll be mutating them to update the sequence number
                        var command = (byte[])_commandBytes.Clone();
                        var reply = (byte[])replyBytes.Clone();

                        // We are doing the span magic here because unlike replies, commands seem to have
                        // at index [0] a "driver byte", the intention of which is somewhat unclear at the
                        // moment
                        ResetMsgSeqNumber(command.AsSpan().Slice(1), seq);
                        ResetMsgSeqNumber(reply.AsSpan(), seq);

                        _parent.Setup(x => x.WriteAsync(It.Is<byte[]>(
                                a => a.SequenceEqual(command)
                            ))).Returns(
                                async (byte[] _) => await _parent._incomingData.Writer.WriteAsync(reply)
                            );
                    }
                }

                private static void ResetMsgSeqNumber(Span<byte> msg, byte seq)
                {
                    Debug.Assert(seq < 4);

                    // The way we use OSDP Protocol, we have a common message format for all commands and
                    // their replies:
                    //   1. Bits 0-1 of byte [5] contain the sequence number of the message
                    //   2. Last two bytes contain CRC of the message.
                    // Therefore what we do here is an in-place update of the sequence number and then
                    // recalculate the last two bytes
                    msg[4] &= 0xfc;
                    msg[4] |= seq;
                    Message.AddCrc(msg);
                }

                private readonly MockConnection _parent;
                private readonly byte[] _commandBytes;
            }

            private readonly Pipe _incomingData = new();
        }
    }
}