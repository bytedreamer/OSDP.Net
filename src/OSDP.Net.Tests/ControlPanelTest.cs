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
            panel.Shutdown();

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