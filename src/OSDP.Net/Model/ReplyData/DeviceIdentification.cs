using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// The PD identification data sent as a reply.
    /// </summary>
    public class DeviceIdentification : ReplyData
    {
        /// <summary>
        /// Creates a new instance of DeviceIdentification
        /// </summary>
        public DeviceIdentification()
        {
        }

        /// <summary>Gets the vendor code.</summary>
        public IEnumerable<byte> VendorCode { set; get; }

        /// <summary>Gets the model number.</summary>
        public byte ModelNumber { set; get;  }

        /// <summary>Gets the hardware version.</summary>
        public byte Version { set; get; }

        /// <summary>Gets the serial number.</summary>
        public int SerialNumber { set; get; }

        /// <summary>
        /// Gets the firmware major version.
        /// </summary>
        public byte FirmwareMajor { set; get; }

        /// <summary>
        /// Gets the firmware minor version.
        /// </summary>
        public byte FirmwareMinor { set; get; }

        /// <summary>
        /// Gets the firmware build.
        /// </summary>
        public byte FirmwareBuild { set; get; }

        /// <inheritdoc/>
        public override ReplyType ReplyType => ReplyType.PdIdReport;

        public static DeviceIdentification ParseData(ReadOnlySpan<byte> data)
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

        /// <inheritdoc/>
        public override byte[] BuildData(bool withPadding = false)
        {
            var buffer = new byte[withPadding ? 16 : 12];

            VendorCode.Take(3).ToArray().CopyTo(buffer, 0);
            buffer[3] = ModelNumber;
            buffer[4] = Version;
            Message.ConvertIntToBytes(SerialNumber).ToArray().CopyTo(buffer, 5);
            buffer[9] = FirmwareMajor;
            buffer[10] = FirmwareMinor;
            buffer[11] = FirmwareBuild;
            if (withPadding) buffer[12] = Message.FirstPaddingByte;

            return buffer;
        }

        /// <inheritdoc />
        public override string ToString() => ToString(0);

        public string ToString(int indent = 4)
        {
            string padding = new string(' ', indent);

            var build = new StringBuilder();
            build.AppendLine($"{padding}     Vendor Code: {BitConverter.ToString(VendorCode.ToArray())}");
            build.AppendLine($"{padding}    Model Number: {ModelNumber}");
            build.AppendLine($"{padding}         Version: {Version}");
            build.AppendLine($"{padding}   Serial Number: {BitConverter.ToString(Message.ConvertIntToBytes(SerialNumber).ToArray())}");
            build.AppendLine($"{padding}Firmware Version: {FirmwareMajor}.{FirmwareMinor}.{FirmwareBuild}");

            return build.ToString();
        }
    }
}