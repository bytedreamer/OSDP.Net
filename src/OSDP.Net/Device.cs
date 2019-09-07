using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OSDP.Net.Messages;

namespace OSDP.Net
{
    internal class Device : IComparer<byte>
    {
        private readonly ConcurrentQueue<Command> _commands = new ConcurrentQueue<Command>();
        private readonly Comparer _comparer = new Comparer(CultureInfo.InvariantCulture);
        private readonly SecureChannel _secureChannel = new SecureChannel();
        private readonly bool _useSecureChannel;

        public Device(byte address, bool useCrc, bool useSecureChannel)
        {
            _useSecureChannel = useSecureChannel;
            Address = address;
            MessageControl = new Control(0, useCrc, useSecureChannel);
            _commands.Enqueue(new PollCommand(Address));
        }

        public byte Address { get; }

        public Control MessageControl { get; }

        public bool IsSecurityEstablished => _secureChannel.IsEstablished;

        /// <inheritdoc />
        public int Compare(byte x, byte y)
        {
            return _comparer.Compare(x, y);
        }

        public Command GetNextCommandData()
        {
            if (_useSecureChannel && !_secureChannel.IsInitialized)
            {
                return new SecurityInitializationRequestCommand(Address, _secureChannel.ServerRandomNumber().ToArray());
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

        public IEnumerable<byte> InitializeSecureChannel(Reply reply)
        {
            var replyData = reply.ExtractReplyData.ToArray();
            
            return _secureChannel.Initialize(replyData.Take(8).ToArray(),
                replyData.Skip(8).Take(8).ToArray(),
                replyData.Skip(16).Take(16).ToArray());
        }

        public bool ValidateSecureChannelEstablishment(Reply reply)
        {
            if (!reply.SecureCryptogramHasBeenAccepted())
            {
                return false;
            }
            
            _secureChannel.Establish(reply.ExtractReplyData.ToArray());

            return true;
        }

        public IEnumerable<byte> GenerateMac(byte[] message, bool isCommand)
        {
            return _secureChannel.GenerateMac(message, isCommand);
        }

        public void ResetSecurity()
        {
            _secureChannel.Reset();
        }
    }
}