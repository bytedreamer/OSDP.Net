using System;
using System.Collections.Generic;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Command to instruct PD to perform a cryptographic challenge/response sequence
    /// </summary>
    internal class AuthenticationChallengeFragment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileTransferFragment"/> class.
        /// </summary>
        /// <param name="totalSize">File size as little-endian format</param>
        /// <param name="offset">Offset of the current message</param>
        /// <param name="fragmentSize">Size of the fragment</param>
        /// <param name="dataFragment">Authentication challenge fragment data</param>
        public AuthenticationChallengeFragment(int totalSize, int offset, ushort fragmentSize, byte[] dataFragment)
        {
            TotalSize = totalSize;
            Offset = offset;
            FragmentSize = fragmentSize;
            DataFragment = dataFragment;
        }

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
        /// Get the authentication challenge fragment data
        /// </summary>
        public byte[] DataFragment { get; }

        internal ReadOnlySpan<byte> BuildData()
        {
            var data = new List<byte>();
            data.AddRange(Message.ConvertIntToBytes(TotalSize));
            data.AddRange(Message.ConvertIntToBytes(Offset));
            data.AddRange(Message.ConvertShortToBytes(FragmentSize));
            data.AddRange(DataFragment);
            return data.ToArray();
        }

        /// <summary>Parses the message payload bytes</summary>
        /// <param name="data">Message payload as bytes</param>
        /// <returns>An instance of FileTransferFragment representing the message payload</returns>
        public static AuthenticationChallengeFragment ParseData(ReadOnlySpan<byte> data)
        {
            return new AuthenticationChallengeFragment(
                Message.ConvertBytesToInt(data.Slice(0, 4).ToArray()),
                Message.ConvertBytesToInt(data.Slice(4, 4).ToArray()),
                Message.ConvertBytesToUnsignedShort(data.Slice(8, 2).ToArray()),
                data.Slice(10).ToArray());
        }
    }
}