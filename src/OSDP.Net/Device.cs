using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using OSDP.Net.Messages;

namespace OSDP.Net
{
    public class Device : IComparer<byte>
    {
        private readonly Comparer _comparer = new Comparer(CultureInfo.InvariantCulture);
        private readonly ConcurrentQueue<Command> _commands = new ConcurrentQueue<Command>();
        
        public Device(byte address)
        {
            Address = address;
            MessageControl = new Control(0, true, false);
            _commands.Enqueue(new PollCommand(Address, MessageControl));
        }

        private byte Address { get; }

        private Control MessageControl { get; }

        public Command GetNextCommandData()
        {
            if (!_commands.TryPeek(out var command))
            {
                command = new PollCommand(Address, MessageControl);
            }

            return command;
        }

        public void ValidReplyHasBeenReceived()
        {
            _commands.TryDequeue(out var command);
            MessageControl.IncrementSequence();
        }

        /// <inheritdoc />
        public int Compare(byte x, byte y)
        {
            return _comparer.Compare(x, y);
        }
    }
}