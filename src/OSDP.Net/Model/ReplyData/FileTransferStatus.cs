using System;
using System.Collections.Generic;
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
        private FileTransferStatus()
        {
        }

        /// <summary>Gets the control flags.</summary>
        public byte Action { get; private set; }

        /// <summary>Gets the request ACU time delay in milliseconds before next osdp_FILETRANSFER command.</summary>
        public ushort RequestedDelay { get;private set;  }

        /// <summary>Gets the file transfer status.</summary>
        public short StatusDetail { get; private set; }

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
                StatusDetail = Message.ConvertBytesToShort(dataArray.Skip(3).Take(2).ToArray()),
                UpdateMessageMaximum = Message.ConvertBytesToUnsignedShort(dataArray.Skip(5).Take(2).ToArray()),
            };
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"             Action: {Action}");
            build.AppendLine($"    Requested Delay: {RequestedDelay}");
            build.AppendLine($"      Status Detail: {StatusDetail}");
            build.AppendLine($" Update Message Max: {UpdateMessageMaximum}");

            return build.ToString();
        }
    }
}