﻿using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace OSDP.Net
{
    public class SerialPortOsdpConnection : IOsdpConnection
    {
        private readonly SerialPort _serialPort = new SerialPort();

        public bool IsOpen => _serialPort.IsOpen;

        public void Open()
        {
            _serialPort.PortName = "/dev/tty.SLAB_USBtoUART";

            _serialPort.Open();
        }

        public void Close()
        {
            _serialPort.Close();
        }

        public async Task WriteAsync(byte[] buffer)
        {
            await _serialPort.BaseStream.WriteAsync(buffer, 0, buffer.Length);
        }

        public async Task<int> ReadAsync(byte[] buffer, CancellationToken token)
        {
            return await _serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length, token);
        }
    }
}
