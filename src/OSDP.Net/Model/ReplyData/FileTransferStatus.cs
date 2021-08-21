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
        public enum StatusDetail
        {
            UnknownError = -4,
            FileDataUnacceptable = -3,
            UnrecognizedFileContents = -2,
            AbortFileTransfer = -1,
            OkToProceed = 0,
            FileContentsProcessed = 1,
            RebootingNow = 2,
            FinishingFileTransfer = 3,
            UnknownStatus = 4
        }

        private FileTransferStatus()
        {
        }

        /// <summary>Gets the control flags.</summary>
        public byte Action { get; private set; }

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
                Action = dataArray[0],
                RequestedDelay = Message.ConvertBytesToUnsignedShort(dataArray.Skip(1).Take(2).ToArray()),
                Detail = SetStatusDetailDefault(Message.ConvertBytesToShort(dataArray.Skip(3).Take(2).ToArray())),
                UpdateMessageMaximum = Message.ConvertBytesToUnsignedShort(dataArray.Skip(5).Take(2).ToArray()),
            };
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"             Action: {Action}");
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