using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDP.Net.Model.CommandData
{
    public class ExtendedWrite
    {
        public ExtendedWrite(byte mode, byte pCommand, byte[] pData)
        {
            Mode = mode;
            PCommand = pCommand;
            PData = pData;
        }

        public byte Mode { get; }
        public byte PCommand { get; }
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
        /// <param name="readerNumber"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public static ExtendedWrite ModeOnePassAPDUCommand(byte readerNumber, byte[] command)
        {
            return new ExtendedWrite(1, 1, command.Prepend(readerNumber).ToArray());
        }

        /// <summary>
        /// Notifies the designated reader to terminate its connection to the Smart Card
        /// </summary>
        /// <param name="readerNumber"></param>
        /// <returns></returns>
        public static ExtendedWrite ModeOneTerminateSmartCardConnection(byte readerNumber)
        {
            return new ExtendedWrite(1, 2, new [] {readerNumber});
        }

        /// <summary>
        /// Instructs the designated reader to perform a Smart Card Scan
        /// </summary>
        /// <param name="readerNumber"></param>
        /// <returns></returns>
        public static ExtendedWrite ModeOneSmartCardScan(byte readerNumber)
        {
            return new ExtendedWrite(1, 4, new [] {readerNumber});
        }

        public ReadOnlySpan<byte> BuildData()
        {
            var data = new List<byte> {Mode, PCommand};
            data.AddRange(PData);
            return data.ToArray();
        }
    }
}