using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;
using System;
using System.Linq;
using System.Text;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// A input status report reply.
    /// </summary>
    public class InputStatus : PayloadData
    {
        /// <summary>
        /// Initializes a new instance of InputStatus class
        /// </summary>
        /// <param name="statuses"></param>
        public InputStatus(bool[] statuses)
        {
            InputStatuses = statuses;
        }

        /// <inheritdoc />
        public override byte Code => (byte)ReplyType.InputStatusReport;

        /// <summary>
        /// Gets the all the PD's input statuses as an array ordered by input number.
        /// </summary>
        public bool[] InputStatuses { get; }

        /// <inheritdoc />
        public override ReadOnlySpan<byte> SecurityControlBlock() => SecurityBlock.ReplyMessageWithDataSecurity;

        /// <summary>
        /// Parses the data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>A input status report reply.</returns>
        internal static InputStatus ParseData(ReadOnlySpan<byte> data)
        {
            return new InputStatus(data.ToArray().Select(Convert.ToBoolean).ToArray());
        }

        /// <inheritdoc />
        public override byte[] BuildData() => InputStatuses.Select(x => x ? (byte)0x00 : (byte)0x01).ToArray();

        /// <inheritdoc />
        public override string ToString()
        {
            byte inputNumber = 0;
            var build = new StringBuilder();
            foreach (bool inputStatus in InputStatuses)
            {
                build.AppendLine($"Input Number {inputNumber++:00}: {inputStatus}");
            }

            return build.ToString();
        }
    }
}