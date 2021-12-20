using System;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// The PD file transfer status sent as a reply.
    /// </summary>
    public class FileTransferStatus
    {
        /// <summary>
        /// Control Flags
        /// </summary>
        [Flags]
        internal enum ControlFlags
        {
            Interleave = 0x1,
            LeaveSecureChannel = 0x2,
            PollResponseAvailable = 0x4
        }

        /// <summary>
        /// Status Detail
        /// </summary>
        public enum StatusDetail
        {
            /// <summary>
            /// The error is unknown
            /// </summary>
            UnknownError = -4,
            /// <summary>
            /// The file data unacceptable (malformed).
            /// </summary>
            FileDataUnacceptable = -3,
            /// <summary>
            /// Unrecognized file contents.
            /// </summary>
            UnrecognizedFileContents = -2,
            /// <summary>
            /// Abort file transfer.
            /// </summary>
            AbortFileTransfer = -1,
            /// <summary>
            /// OK to proceed.
            /// </summary>
            OkToProceed = 0,
            /// <summary>
            /// The File contents processed.
            /// </summary>
            FileContentsProcessed = 1,
            /// <summary>
            /// Rebooting now, expect full communications reset.
            /// </summary>
            RebootingNow = 2,
            /// <summary>
            /// PD is finishing file transfer.
            /// </summary>
            FinishingFileTransfer = 3,
            /// <summary>
            /// The status is unknown
            /// </summary>
            UnknownStatus = 4
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="FileTransferStatus"/> class from being created.
        /// </summary>
        private FileTransferStatus()
        {
        }

        /// <summary>Gets the control flags.</summary>
        internal ControlFlags Action { get; private set; }

        /// <summary>Gets the request ACU time delay in milliseconds before next osdp_FILETRANSFER command.</summary>
        public ushort RequestedDelay { get;private set;  }

        /// <summary>Gets the file transfer status.</summary>
        public StatusDetail Detail { get; private set; }

        /// <summary>Gets the alternative maximum message size.</summary>
        public ushort UpdateMessageMaximum { get; private set; }

        internal static FileTransferStatus ParseData(ReadOnlySpan<byte> data)
        {
            var dataArray = data.ToArray();
            if (dataArray.Length != 7)
            {
                throw new Exception("Invalid size for the data");
            }

            return new FileTransferStatus
            {
                Action = (ControlFlags)dataArray[0],
                RequestedDelay = Message.ConvertBytesToUnsignedShort(dataArray.Skip(1).Take(2).ToArray(), true),
                Detail = SetStatusDetailDefault(Message.ConvertBytesToShort(dataArray.Skip(3).Take(2).ToArray(), true)),
                UpdateMessageMaximum = Message.ConvertBytesToUnsignedShort(dataArray.Skip(5).Take(2).ToArray(), true)
            };
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"             Action: {Action:G}");
            build.AppendLine($"    Requested Delay: {RequestedDelay}");
            build.AppendLine($"      Status Detail: {Detail}");
            build.AppendLine($" Update Message Max: {UpdateMessageMaximum}");

            return build.ToString();
        }

        private static StatusDetail SetStatusDetailDefault(short value)
        {
            return value switch
            {
                < -3 => StatusDetail.UnknownError,
                > 3 => StatusDetail.UnknownStatus,
                _ => (StatusDetail)value
            };
        }
    }
}