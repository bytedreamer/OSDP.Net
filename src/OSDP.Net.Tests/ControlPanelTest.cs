using System;
using System.Diagnostics;
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
            var mockConnection = new MockConnection();
            
            var panel = new ControlPanel();
            Guid id = panel.StartConnection(mockConnection.Object);
            panel.AddDevice(id, 0, true, false);

            // Act
            // Assert
            await TaskEx.WaitUntil(() => panel.IsOnline(id, 0), TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(3));
        }

        [Test]
        public async Task ShutdownTest()
        {
            // Arrange
            var mockConnection = new MockConnection();
            mockConnection.Object.Open();
            
            var panel = new ControlPanel();
            Guid id = panel.StartConnection(mockConnection.Object);
            panel.AddDevice(id, 0, true, false);

            // Act
            await panel.Shutdown();

            // Assert
            await TaskEx.WaitUntil(() => mockConnection.NumberOfTimesCalledOpen == 1, TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(3));
            await TaskEx.WaitUntil(() => mockConnection.NumberOfTimesCalledClose == 1, TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(3));
        }

        [Test]
        public async Task StartConnectionTest()
        {
            // Arrange
            var mockConnection = new MockConnection();
            var panel = new ControlPanel();

            // Act
            Guid id = panel.StartConnection(mockConnection.Object);
            panel.AddDevice(id, 0, true, false);

            // Assert
            await TaskEx.WaitUntil(() => mockConnection.NumberOfTimesCalledOpen == 1, TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(3));
        }

        [Test]
        public void StartConnectionWithSameConnectionTwiceTest()
        {
            // Arrange
            var mockConnection = new MockConnection();
            var panel = new ControlPanel();
            var id = panel.StartConnection(mockConnection.Object);

            // Act/Assert
            var exception = Assert.Throws<InvalidOperationException>(() => panel.StartConnection(mockConnection.Object), "");
            Assert.That(exception?.Message, Is.EqualTo(
                $"The IOsdpConnection is already active in connection {id}. " +
                    "That connection must be stopped before starting a new one."));
        }

        [Test]
        public void StartSameConnectionConcurrentlyShouldOnlyStartItOnce()
        {
            // Arrange
            var mockConnection = new MockConnection();
            var instance = mockConnection.Object;
            var panel = new ControlPanel();

            // Act
            var tasks = Enumerable
                .Range(0, 100)
                .Select(_ => Task.Run(() => panel.StartConnection(instance)))
                .ToArray();
            try
            {
                // ReSharper disable once CoVariantArrayConversion
                Task.WaitAll(tasks);
            }
            catch { /* We handle errors later */ }

            // Assert
            Assert.That(tasks.Count(t => t.Status == TaskStatus.RanToCompletion), Is.EqualTo(1));
            Assert.That(tasks.Count(t => t.Status == TaskStatus.Faulted), Is.EqualTo(99));
        }

        [Test]
        public void StopSameConnectionConcurrentlyShouldSucceed()
        {
            // Arrange
            var mockConnection = new MockConnection();
            var panel = new ControlPanel();
            var id = panel.StartConnection(mockConnection.Object);

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
            Assert.That(tasks.Count(t => t.Status == TaskStatus.RanToCompletion), Is.EqualTo(100));
        }

        [Test]
        public async Task StartConnectionRestartWithSameConnectionTest()
        {
            // Arrange
            var mockConnection = new MockConnection();
            var panel = new ControlPanel();

            // Act
            var id1 = panel.StartConnection(mockConnection.Object);
            await panel.StopConnection(id1);
            var id2 = panel.StartConnection(mockConnection.Object);
            await panel.StopConnection(id2);

            // Assert
            Assert.That(id1, Is.Not.EqualTo(id2));
        }


        [Test]
        public async Task StopConnectionTest()
        {
            // Arrange
            var mockConnection = new MockConnection();
            var panel = new ControlPanel();

            Guid id = panel.StartConnection(mockConnection.Object);
            panel.AddDevice(id, 0, true, false);

            // Act
            await panel.StopConnection(id);

            // Assert
            await TaskEx.WaitUntil(() => mockConnection.NumberOfTimesCalledOpen == 1, TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(3));
            await TaskEx.WaitUntil(() => mockConnection.NumberOfTimesCalledClose == 1, TimeSpan.FromMilliseconds(100),
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

        private class MockConnection : Mock<IOsdpConnection>
        {
            static readonly byte[] PollCommandBytes = { 255, 83, 0, 8, 0, 4, 96, 235, 170 };
            static readonly byte[] AckReplyBytes = { 83, 128, 8, 0, 5, 64, 104, 159 };

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

                Setup(x => x.Open()).Callback(() => NumberOfTimesCalledOpen++);
                Setup(x => x.Open()).Callback(() => NumberOfTimesCalledClose++);

                // Setup handling for a polling command which always gets issued when connection is alive
                // Here we'll just reply with a generic ACK to signal to ACU that the command was successfully
                // handled.
                OnCommand(PollCommandBytes).Reply(AckReplyBytes);
            }

            public ExpectedCommand OnCommand(byte[] commandBytes) => new(this, commandBytes);
            
            /// <summary>
            /// Number of times the Open method is called
            /// </summary>
            public int NumberOfTimesCalledOpen { get; private set; }
            
            /// <summary>
            /// Number of times the Close method is called
            /// </summary>
            public int NumberOfTimesCalledClose { get; private set; }
            
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