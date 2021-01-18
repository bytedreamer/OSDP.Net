using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// Device identification data
    /// </summary>
    public class DeviceIdentification
    {
        private DeviceIdentification()
        {
        }

        /// <summary>Gets the vendor code.</summary>
        public IEnumerable<byte> VendorCode { get; private set; }

        /// <summary> Gets the model number. </summary>
        public byte ModelNumber { get;private set;  }

        /// <summary> Gets the hardware version. </summary>
        public byte Version { get; private set; }

        /// <summary>Gets the serial number.</summary>
        public int SerialNumber { get; private set; }

        /// <summary>
        /// Gets the firmware major version.
        /// </summary>
        public byte FirmwareMajor { get; private set; }

        /// <summary>
        /// Gets the firmware minor version.
        /// </summary>
        public byte FirmwareMinor { get; private set; }

        /// <summary>
        /// Gets the firmware build.
        /// </summary>
        public byte FirmwareBuild { get; private set; }


        internal static DeviceIdentification ParseData(ReadOnlySpan<byte> data)
        {
            var dataArray = data.ToArray();
            if (dataArray.Length != 12)
            {
                throw new Exception("Invalid size for the data");
            }

            var deviceIdentification = new DeviceIdentification
            {
                VendorCode = dataArray.Take(3),
                ModelNumber = dataArray[3],
                Version = dataArray[4],
                SerialNumber = Message.ConvertBytesToInt(dataArray.Skip(5).Take(4).ToArray()),
                FirmwareMajor = dataArray[9],
                FirmwareMinor = dataArray[10],
                FirmwareBuild = dataArray[11]
            };

            return deviceIdentification;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"     Vendor Code: {BitConverter.ToString(VendorCode.ToArray())}");
            build.AppendLine($"    Model Number: {ModelNumber}");
            build.AppendLine($"         Version: {Version}");
            build.AppendLine($"   Serial Number: {BitConverter.ToString(Message.ConvertIntToBytes(SerialNumber).ToArray())}");
            build.AppendLine($"Firmware Version: {FirmwareMajor}.{FirmwareMinor}.{FirmwareBuild}");

            return build.ToString();
        }
    }
}