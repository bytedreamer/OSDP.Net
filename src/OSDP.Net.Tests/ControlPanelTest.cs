using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OSDP.Net.Connections;
using OSDP.Net.Messages;
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
            Assert.That(ex.Message, Is.EqualTo(
                $"The IOsdpConnection is already active in connection {id}. " +
                    "That connection must be stopped before starting a new one."));
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

        class TestConnection : IOsdpConnection
        {
            private readonly MemoryStream _stream = new MemoryStream();

            public int NumberOfTimesCalledClose { get; private set; }

            public int NumberOfTimesCalledOpen { get; private set; }

            public int BaudRate => 9600;

            public bool IsOpen => true;

            public TimeSpan ReplyTimeout { get; set; } = TimeSpan.FromSeconds(1);

            public void Open()
            {
                NumberOfTimesCalledOpen++;
            }

            public void Close()
            {
                NumberOfTimesCalledClose++;
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
    }
}