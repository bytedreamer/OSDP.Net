using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using OSDP.Net.Messages;

namespace OSDP.Net
{
    internal class Device : IComparable<Device>
    {
        private readonly ConcurrentQueue<Command> _commands = new ConcurrentQueue<Command>();

        private readonly byte[] _defaultKey = {
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F
        };

        private readonly SecureChannel _secureChannel = new SecureChannel();

        private DateTime _lastValidReply = DateTime.MinValue;

        public Device(byte address, bool useCrc, bool useSecureChannel, byte[] secureChannelKey)
        {
            UseSecureChannel = useSecureChannel;
            Address = address;
            MessageControl = new Control(0, useCrc, useSecureChannel);

            switch (UseSecureChannel)
            {
                case true when secureChannelKey == null:
                    SecureChannelKey = _defaultKey;
                    break;
                case true:
                    SecureChannelKey = secureChannelKey;
                    break;
            }

            if (UseSecureChannel) IsDefaultKey = _defaultKey.SequenceEqual(SecureChannelKey ?? Array.Empty<byte>());
        }

        internal byte[] SecureChannelKey { get; }

        private bool IsDefaultKey { get; }

        public byte Address { get; }

        public Control MessageControl { get; }

        public bool UseSecureChannel { get; }

        public bool IsSecurityEstablished => MessageControl.HasSecurityControlBlock && _secureChannel.IsEstablished;

        public bool IsConnected => _lastValidReply + TimeSpan.FromSeconds(5) >= DateTime.UtcNow &&
                                   (!MessageControl.HasSecurityControlBlock || IsSecurityEstablished);

        /// <inheritdoc />
        public int CompareTo(Device other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Address.CompareTo(other.Address);
        }

        public Command GetNextCommandData()
        {
            if (UseSecureChannel && !_secureChannel.IsInitialized)
            {
                return new SecurityInitializationRequestCommand(Address, _secureChannel.ServerRandomNumber().ToArray(), IsDefaultKey);
            }

            if (MessageControl.Sequence == 0)
            {
                return new PollCommand(Address);
            }

            if (UseSecureChannel && !_secureChannel.IsEstablished)
            {
                return new ServerCryptogramCommand(Address, _secureChannel.ServerCryptogram, IsDefaultKey);
            }

            if (!_commands.TryDequeue(out var command))
            {
                return new PollCommand(Address);
            }

            return command;
        }

        public void SendCommand(Command command)
        {
            _commands.Enqueue(command);
        }

        public void ValidReplyHasBeenReceived(byte sequence)
        {
            MessageControl.IncrementSequence(sequence);
            
            // It's valid once sequences are above zero
            if (sequence > 0) _lastValidReply = DateTime.UtcNow;
        }

        public void InitializeSecureChannel(Reply reply)
        {
            var replyData = reply.ExtractReplyData.ToArray();

            _secureChannel.Initialize(replyData.Skip(8).Take(8).ToArray(),
                replyData.Skip(16).Take(16).ToArray(), SecureChannelKey);
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

        public ReadOnlySpan<byte> GenerateMac(ReadOnlySpan<byte> message, bool isCommand)
        {
            return _secureChannel.GenerateMac(message, isCommand);
        }

        public ReadOnlySpan<byte> EncryptData(ReadOnlySpan<byte> data)
        {
            return _secureChannel.EncryptData(data);
        }

        public IEnumerable<byte> DecryptData(ReadOnlySpan<byte> data)
        {
            return _secureChannel.DecryptData(data);
        }

        public void CreateNewRandomNumber()
        {
            _secureChannel.CreateNewRandomNumber();
        }
    }
}