using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using OSDP.Net.Messages;

namespace OSDP.Net
{
    public class Device : IComparer<byte>
    {
        private readonly ConcurrentQueue<Command> _commands = new ConcurrentQueue<Command>();
        private readonly Comparer _comparer = new Comparer(CultureInfo.InvariantCulture);

        public Device(byte address)
        {
            Address = address;
            MessageControl = new Control(0, true, false);
            _commands.Enqueue(new PollCommand(Address, MessageControl));
        }

        public byte Address { get; }

        private Control MessageControl { get; }

        /// <inheritdoc />
        public int Compare(byte x, byte y)
        {
            return _comparer.Compare(x, y);
        }

        public Command GetNextCommandData()
        {
            if (!_commands.TryPeek(out var command))
            {
                command = new PollCommand(Address, MessageControl);
            }

            return command;
        }

        public void SendCommand(Command command)
        {
            command.Control = MessageControl;
            _commands.Enqueue(command);
        }

        public void ValidReplyHasBeenReceived()
        {
            _commands.TryDequeue(out _);
            MessageControl.IncrementSequence();
        }
    }
}