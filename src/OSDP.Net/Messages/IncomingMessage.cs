﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDP.Net.Messages
{
    /// <summary>
    /// Represents a message that was received from the wire. It extends base Message
    /// class with extra properties/methods that specifically indicate the parsing and
    /// validation of incoming raw bytes.
    /// </summary>
    public class IncomingMessage : Message
    {
        private const ushort MessageHeaderSize = 6;
        private readonly byte[] _origMessage;

        /// <summary>
        /// Creates a new instance of IncomingMessage class
        /// </summary>
        /// <param name="data">Raw byte data received from the wire</param>
        /// <param name="channel">Message channel context</param>
        /// <param name="connectionId">ID of the connection</param>
        public IncomingMessage(ReadOnlySpan<byte> data, IMessageChannel channel, Guid connectionId)
        {
            // TODO: way too much copying in this code, simplify it.
            _origMessage = data.ToArray();

            Address = (byte)(data[1] & AddressMask);
            Sequence = (byte)(data[4] & 0x03);
            IsUsingCrc = Convert.ToBoolean(data[4] & 0x04);
            ushort replyMessageFooterSize = (ushort)(IsUsingCrc ? 2 : 1);
            bool isSecureControlBlockPresent = Convert.ToBoolean(data[4] & 0x08);
            byte secureBlockSize = (byte)(isSecureControlBlockPresent ? data[5] : 0);
            SecurityBlockType = (byte)(isSecureControlBlockPresent ? data[6] : 0);
            int messageLength = data.Length - (IsUsingCrc ? 6 : 5);
            if (isSecureControlBlockPresent)
            {
                SecureBlockData = data.Slice(MessageHeaderSize + 1, secureBlockSize - 2).ToArray();
                Mac = data.Slice(messageLength, MacSize).ToArray();
            }

            Type = data[MsgTypeIndex + secureBlockSize];

            Payload = data.Slice(MessageHeaderSize + secureBlockSize, data.Length -
                MessageHeaderSize - secureBlockSize - replyMessageFooterSize -
                (IsSecureMessage ? MacSize : 0)).ToArray();
            if (SecurityBlockType == (byte)Messages.SecurityBlockType.ReplyMessageWithDataSecurity)
            {
                // TODO: will need to figure out how to handle security when there's no Device instance present

                //Payload = device.DecryptData(Payload.ToArray()).ToArray();
                throw new NotImplementedException();
            }

            IsDataCorrect = IsUsingCrc
                ? CalculateCrc(data.Slice(0, data.Length - 2)) ==
                  ConvertBytesToUnsignedShort(data.Slice(data.Length - 2, 2))
                : CalculateChecksum(data.Slice(0, data.Length - 1).ToArray()) == data[data.Length - 1];

            if (IsSecureMessage)
            {
                // TODO: What if this is not a command??
                var mac = channel.GenerateMac(data.Slice(0, messageLength).ToArray(), true);
                IsValidMac = !IsSecureMessage || mac.Slice(0, MacSize).SequenceEqual(Mac?.ToArray());
            }
            else
            {
                IsValidMac = true;
            }

            ConnectionId = connectionId;
        }

        /// <summary>
        /// If true, the message has a CRC suffix; otherwise it has a checksum
        /// </summary>
        public bool IsUsingCrc { get; }

        /// <summary>
        /// Command/reply code of the message
        /// </summary>
        public byte Type { get; }

        /// <summary>
        /// Message sequence number
        /// </summary>
        public byte Sequence { get; }

        /// <summary>
        /// Indicates if the message was sent via an established secure channel
        /// </summary>
        public bool IsSecureMessage => SecureSessionMessages.Contains(SecurityBlockType) && IsDataSecure;

        /// <summary>
        /// Indicates if the message has a valid MAC signature which was validated via
        /// local message channel context
        /// </summary>
        public bool IsValidMac { get; }

        /// <summary>
        /// Returns the raw message payload
        /// </summary>
        public byte[] Payload { get; }

        /// <summary>
        /// Original byte data which includes the header, security control block, payload, 
        /// MAC and CRC/Checksum suffix
        /// </summary>
        public ReadOnlySpan<byte> OriginalMsgData => _origMessage;

        /// <summary>
        /// ID of the connection on which the channel was received
        /// (not entirely sure if this is needed here)
        /// </summary>
        public Guid ConnectionId { get; }

        /// <summary>
        /// Type of the security block, if there is one
        /// </summary>
        public byte SecurityBlockType { get; }

        /// <summary>
        /// Raw security block bytes
        /// </summary>
        protected IEnumerable<byte> SecureBlockData { get; }

        /// <inheritdoc/>
        protected override ReadOnlySpan<byte> Data() => Payload.ToArray();

        private bool IsDataSecure => Payload == null || Payload.Length == 0 || 
            SecurityBlockType == (byte)Messages.SecurityBlockType.ReplyMessageWithDataSecurity || 
            SecurityBlockType == (byte)Messages.SecurityBlockType.CommandMessageWithDataSecurity;
        private IEnumerable<byte> Mac { get; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private bool IsDataCorrect { get; }
        private static IEnumerable<byte> SecureSessionMessages => new[]
        {
            (byte)Messages.SecurityBlockType.CommandMessageWithNoDataSecurity,
            (byte)Messages.SecurityBlockType.ReplyMessageWithNoDataSecurity,
            (byte)Messages.SecurityBlockType.CommandMessageWithDataSecurity,
            (byte)Messages.SecurityBlockType.ReplyMessageWithDataSecurity,
        };
    }
}