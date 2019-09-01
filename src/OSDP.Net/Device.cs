using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using OSDP.Net.Messages;

namespace OSDP.Net
{
    internal class Device : IComparer<byte>
    {
        private readonly ConcurrentQueue<Command> _commands = new ConcurrentQueue<Command>();
        private readonly Comparer _comparer = new Comparer(CultureInfo.InvariantCulture);
        private readonly bool _useSecureChannel;
        private byte[] _hostCryptogram = { };

        public Device(byte address, bool useSecureChannel)
        {
            _useSecureChannel = useSecureChannel;
            Address = address;
            MessageControl = new Control(0, true, useSecureChannel);
            _commands.Enqueue(new PollCommand(Address));
        }

        public byte Address { get; }

        public Control MessageControl { get; }

        /// <inheritdoc />
        public int Compare(byte x, byte y)
        {
            return _comparer.Compare(x, y);
        }

        public Command GetNextCommandData()
        {
            if (_useSecureChannel && _hostCryptogram.Length == 0)
            {
                return new SecurityInitializationRequestCommand(Address);
            }
            
            if (!_commands.TryPeek(out var command))
            {
                command = new PollCommand(Address);
            }

            return command;
        }

        public void SendCommand(Command command)
        {
            _commands.Enqueue(command);
        }

        public void ValidReplyHasBeenReceived()
        {
            _commands.TryDequeue(out _);
            MessageControl.IncrementSequence();
        }
    }
}