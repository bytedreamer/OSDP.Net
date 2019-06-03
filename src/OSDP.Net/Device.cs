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
        private readonly ConcurrentQueue<CommandBase> _commands = new ConcurrentQueue<CommandBase>();
        
        public Device(byte address)
        {
            Address = address;
            MessageControl = new Control(0, true, false);
            _commands.Enqueue(new PollCommand());
        }

        private byte Address { get; }

        private Control MessageControl { get; }

        public IEnumerable<byte> GetNextCommandData()
        {
            if (!_commands.TryDequeue(out var command))
            {
                command = new PollCommand();
            }

            var commandData = command.BuildCommand(Address, MessageControl);
            MessageControl.IncrementSequence();
            return commandData;
        }

        /// <inheritdoc />
        public int Compare(byte x, byte y)
        {
            return _comparer.Compare(x, y);
        }
    }
}