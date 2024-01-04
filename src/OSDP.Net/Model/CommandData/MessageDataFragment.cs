using System;
using System.Collections.Generic;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Represents a fragment of data for a multi-part message.
    /// </summary>
    internal class MessageDataFragment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageDataFragment"/> class.
        /// </summary>
        /// <param name="totalSize">File size as little-endian format</param>
        /// <param name="offset">Offset of the current message</param>
        /// <param name="fragmentSize">Size of the fragment</param>
        /// <param name="dataFragment">Authentication challenge fragment data</param>
        public MessageDataFragment(ushort totalSize, ushort offset, ushort fragmentSize, byte[] dataFragment)
        {
            TotalSize = totalSize;
            Offset = offset;
            FragmentSize = fragmentSize;
            DataFragment = dataFragment;
        }

        /// <summary>
        /// Get the total message data size as little-endian format
        /// </summary>
        public ushort TotalSize { get; }

        /// <summary>
        /// Get the offset of the current message
        /// </summary>
        public ushort Offset { get; }

        /// <summary>
        /// Get the size of the fragment
        /// </summary>
        public ushort FragmentSize { get; }

        /// <summary>
        /// Get the fragment data
        /// </summary>
        public byte[] DataFragment { get; }

        internal ReadOnlySpan<byte> BuildData()
        {
            var data = new List<byte>();
            data.AddRange(Message.ConvertShortToBytes(TotalSize));
            data.AddRange(Message.ConvertShortToBytes(Offset));
            data.AddRange(Message.ConvertShortToBytes(FragmentSize));
            data.AddRange(DataFragment);
            return data.ToArray();
        }

        /// <summary>Parses the message payload bytes</summary>
        /// <param name="data">Message payload as bytes</param>
        /// <returns>An instance of MessageDataFragment representing the message payload</returns>
        public static MessageDataFragment ParseData(ReadOnlySpan<byte> data)
        {
            return new MessageDataFragment(
                Message.ConvertBytesToUnsignedShort(data.Slice(0, 4).ToArray()),
                Message.ConvertBytesToUnsignedShort(data.Slice(4, 4).ToArray()),
                Message.ConvertBytesToUnsignedShort(data.Slice(8, 2).ToArray()),
                data.Slice(10).ToArray());
        }
    }
}