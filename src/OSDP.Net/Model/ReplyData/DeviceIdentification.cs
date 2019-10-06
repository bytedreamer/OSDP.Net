using System;
using System.Linq;
using System.Net;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    public class DeviceIdentification : ReplyData
    {
        private DeviceIdentification()
        {
        }

        public byte[] VendorCode { get; private set; }
        public byte ModelNumber { get;private set;  }
        public byte Version { get; private set; }
        public int SerialNumber { get; private set; }
        public byte FirmwareMajor { get; private set; }
        public byte FirmwareMinor { get; private set; }
        public byte FirmwareBuild { get; private set; }

        internal static DeviceIdentification CreateDeviceIdentification(Reply reply)
        {
            var data = reply.ExtractReplyData.ToArray();
            if (data.Length != 12)
            {
                throw new Exception("Invalid size for the data");
            }

            var deviceIdentification = new DeviceIdentification
            {
                VendorCode = data.Take(3).ToArray(),
                ModelNumber = data[3],
                Version = data[4],
                SerialNumber = ConvertBytesToInt(data.Skip(5).Take(4).ToArray()),
                FirmwareMajor = data[9],
                FirmwareMinor = data[10],
                FirmwareBuild = data[11]
            };

            return deviceIdentification;
        }

        public override string ToString()
        {
            var build = new StringBuilder();
            build.AppendLine($"     Vendor Code: {BitConverter.ToString(VendorCode)}");
            build.AppendLine($"    Model Number: {ModelNumber}");
            build.AppendLine($"         Version: {Version}");
            build.AppendLine($"   Serial Number: {BitConverter.ToString(ConvertIntToBytes(SerialNumber).ToArray())}");
            build.AppendLine($"Firmware Version: {FirmwareMajor}.{FirmwareMinor}.{FirmwareBuild}");

            return build.ToString();
        }
    }
}