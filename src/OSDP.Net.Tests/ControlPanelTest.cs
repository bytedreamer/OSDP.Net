using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OSDP.Net.Connections;
using OSDP.Net.Messages;

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
            panel.AddDevice(id, 0, TODO, false);

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
            panel.AddDevice(id, 0, TODO, false);

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
            panel.AddDevice(id, 0, TODO, false);

            // Assert
            await TaskEx.WaitUntil(() => connection.NumberOfTimesCalledOpen == 1, TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(3));
        }
    }

    class TestConnection : IOsdpConnection
    {
        private readonly MemoryStream _stream = new MemoryStream();

        public int NumberOfTimesCalledClose { get; private set; }

        public int NumberOfTimesCalledOpen { get; private set; }

        public int BaudRate => 9600;
        public bool IsOpen => true;

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
    
    /// <summary>
    /// Provided by Sinaesthetic at https://stackoverflow.com/a/52357854
    /// </summary>
    internal static class TaskEx
    {
        /// <summary>
        /// Blocks while condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The condition that will perpetuate the block.</param>
        /// <param name="frequency">The frequency at which the condition will be check.</param>
        /// <param name="timeout">Timeout waiting for a the condition to be satisfied.</param>
        /// <exception cref="TimeoutException"></exception>
        /// <returns></returns>
        public static async Task WaitWhile(Func<bool> condition, TimeSpan frequency, TimeSpan timeout)
        {
            var waitTask = new TaskFactory().StartNew(async () =>
            {
                while (condition()) await Task.Delay(frequency);
            });

            if(waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
                throw new TimeoutException();
        }

        /// <summary>
        /// Blocks until condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The break condition.</param>
        /// <param name="frequency">The frequency at which the condition will be checked.</param>
        /// <param name="timeout">Timeout waiting for a the condition to be satisfied.</param>
        /// <returns></returns>
        public static async Task WaitUntil(Func<bool> condition, TimeSpan frequency, TimeSpan timeout)
        {
            var waitTask = new TaskFactory().StartNew(async () =>
            {
                while (!condition()) await Task.Delay(frequency);
            });

            if (waitTask != await Task.WhenAny(waitTask, 
                    Task.Delay(timeout))) 
                throw new TimeoutException();
        }
    }
}