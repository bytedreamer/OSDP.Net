using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// The extended write data.
    /// </summary>
    public class ExtendedWrite
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedWrite"/> class.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="pCommand">The command code dependent on the command.</param>
        /// <param name="pData">The data dependent on the command.</param>
        public ExtendedWrite(byte mode, byte pCommand, byte[] pData)
        {
            Mode = mode;
            PCommand = pCommand;
            PData = pData;
        }

        /// <summary>
        /// Gets the extended READ/WRITE Mode.
        /// </summary>
        public byte Mode { get; }

        /// <summary>
        /// Gets command code dependent on the command.
        /// </summary>
        public byte PCommand { get; }

        /// <summary>
        /// Gets data dependent on the command.
        /// </summary>
        public byte[] PData { get; }

        /// <summary>
        /// Read mode setting
        /// </summary>
        /// <returns></returns>
        public static ExtendedWrite ReadModeSetting()
        {
            return new ExtendedWrite(0, 1, new byte[] { });
        }

        /// <summary>
        /// Set mode 0 configuration
        /// </summary>
        /// <param name="enabled"></param>
        /// <returns></returns>
        public static ExtendedWrite ModeZeroConfiguration(bool enabled)
        {
            return new ExtendedWrite(0, 2, new byte[] {0, (byte)(enabled ? 1 : 0) });
        }

        /// <summary>
        /// Set mode 1 configuration
        /// </summary>
        /// <returns></returns>
        public static ExtendedWrite ModeOneConfiguration()
        {
            return new ExtendedWrite(0, 2, new byte[] {1, 0});
        }

        /// <summary>
        /// Pass the APDU embedded in this command to the specified reader
        /// </summary>
        /// <param name="readerNumber">The reader number starting at 0.</param>
        /// <param name="command"></param>
        /// <returns></returns>
        public static ExtendedWrite ModeOnePassAPDUCommand(byte readerNumber, byte[] command)
        {
            return new ExtendedWrite(1, 1, command.Prepend(readerNumber).ToArray());
        }

        /// <summary>
        /// Notifies the designated reader to terminate its connection to the Smart Card
        /// </summary>
        /// <param name="readerNumber">The reader number starting at 0.</param>
        /// <returns></returns>
        public static ExtendedWrite ModeOneTerminateSmartCardConnection(byte readerNumber)
        {
            return new ExtendedWrite(1, 2, new [] {readerNumber});
        }

        /// <summary>
        /// Instructs the designated reader to perform a Smart Card Scan
        /// </summary>
        /// <param name="readerNumber">The reader number starting at 0.</param>
        /// <returns></returns>
        public static ExtendedWrite ModeOneSmartCardScan(byte readerNumber)
        {
            return new ExtendedWrite(1, 4, new [] {readerNumber});
        }

        /// <summary>
        /// Builds the data.
        /// </summary>
        /// <returns>The data.</returns>
        public ReadOnlySpan<byte> BuildData()
        {
            var data = new List<byte> {Mode, PCommand};
            data.AddRange(PData);
            return data.ToArray();
        }
    }
}