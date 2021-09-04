using System;
using System.Collections.Generic;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Command data to send a data fragment of a file to a PD.
    /// </summary>
    public class FileTransfer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileTransfer"/> class.
        /// </summary>
        /// <param name="type">File transfer type</param>
        /// <param name="totalSize">File size as little-endian format</param>
        /// <param name="offset">Offset of the current message</param>
        /// <param name="fragmentSize">Size of the fragment</param>
        /// <param name="dataFragment">File transfer fragment data</param>
        public FileTransfer(byte type, int totalSize, int offset, ushort fragmentSize, byte[] dataFragment)
        {
            Type = type;
            TotalSize = totalSize;
            Offset = offset;
            FragmentSize = fragmentSize;
            DataFragment = dataFragment;
        }

        /// <summary>
        /// Get the file transfer type
        /// </summary>
        public byte Type { get; }

        /// <summary>
        /// Get the file size as little-endian format
        /// </summary>
        public int TotalSize { get; }

        /// <summary>
        /// Get the offset of the current message
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// Get the size of the fragment
        /// </summary>
        public ushort FragmentSize { get; }

        /// <summary>
        /// Get the file transfer fragment data
        /// </summary>
        public byte[] DataFragment { get; }

        internal ReadOnlySpan<byte> BuildData()
        {
            var data = new List<byte> {Type};
            data.AddRange(Message.ConvertIntToBytes(TotalSize));
            data.AddRange(Message.ConvertIntToBytes(Offset));
            data.AddRange(Message.ConvertShortToBytes(FragmentSize));
            data.AddRange(DataFragment);
            return data.ToArray();
        }
    }
}