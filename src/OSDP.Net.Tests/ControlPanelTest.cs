using System;
using System.IO;
using System.Linq;
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
    }
}