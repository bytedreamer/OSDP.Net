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
        public async Task SendCommandTest()
        {
            // Arrange
            var connection = new TestConnection();
            connection.ReplyValues.Enqueue(0x40);
            connection.ReplyValues.Enqueue(0x45);

            var panel = new ControlPanel();
            Guid id = panel.StartConnection(connection);
            panel.AddDevice(id, 0, false);

            // Act
            var reply = await panel.SendCommand(id, new IdReportCommand(0));

            // Assert
            Assert.AreEqual(reply.Type, ReplyType.PdIdReport);
        }

        [Test]
        public async Task ShutdownTest()
        {
            // Arrange
            var connection = new TestConnection();
            var panel = new ControlPanel();
            Guid id = panel.StartConnection(connection);
            panel.AddDevice(id, 0, false);

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
            panel.AddDevice(id, 0, false);

            // Assert
            await TaskEx.WaitUntil(() => connection.NumberOfTimesCalledOpen == 1, TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(3));
        }
    }

    class TestConnection : IOsdpConnection
    {
        public readonly Queue<byte> ReplyValues = new Queue<byte>();
        private MemoryStream _nextReply;

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

        public Task WriteAsync(byte[] buffer)
        {
            CreateReply(buffer.SkipWhile(b => b!=0x53).ToList());
            return Task.CompletedTask;
        }

        public async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
        {
            return await _nextReply.ReadAsync(buffer, 0, buffer.Length, token);
        }

        private void CreateReply(IReadOnlyList<byte> buffer)
        {
            var replyBuffer = new List<byte>();
            replyBuffer.AddRange(new byte[] {0x53, (byte) (buffer[1] | 0x80), 0x00, 0x00});
            replyBuffer.Add(buffer[4]);
            replyBuffer.AddRange(new byte[] {ReplyValues.Dequeue(), 0x00, 0x00});

            Command.AddPacketLength(replyBuffer);
            Command.AddCrc(replyBuffer);
            _nextReply = new MemoryStream(replyBuffer.ToArray());
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