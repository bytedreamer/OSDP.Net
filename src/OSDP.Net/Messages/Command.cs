using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OSDP.Net.Tests")]
namespace OSDP.Net.Messages
{
    public abstract class Command : Message
    {
        protected abstract byte CommandCode { get; }

        protected abstract ReadOnlySpan<byte> SecurityControlBlock();

        protected abstract void CustomCommandUpdate(Span<byte> commandBuffer);

        internal byte[] BuildCommand(Device device)
        {
            var header = BuildHeader(device);

            byte[] command;
            if (device.IsSecurityEstablished)
            {
                var combined = header.Concat(EncryptedData(device).ToArray()).ToArray();

                // include mac and crc in length before generating mac
                ushort totalLength = (ushort) (4 + (device.MessageControl.UseCrc ? 2 : 1));
                AddPacketLength(combined, totalLength);
                
                command = new byte[combined.Length + totalLength];
                var combinedWithMac = combined.Concat(device.GenerateMac(combined, true).Take(4)).ToArray();
                Buffer.BlockCopy(combinedWithMac, 0, command, 0, combinedWithMac.Length);
            }
            else
            {
                var data = Data();
                int totalLength = header.Length + data.Length + (device.MessageControl.UseCrc ? 2 : 1);
                var pool = MemoryPool<byte>.Shared;
                var buffer = pool.Rent(totalLength);

                try
                {
                    var cursor = buffer.Memory.Span;

                    header.CopyTo(cursor);
                    cursor = cursor.Slice(header.Length);

                    data.CopyTo(cursor);

                    command = buffer.Memory.Slice(0, totalLength).ToArray();
                }
                finally
                {
                    pool.Dispose();
                }
            }

            AddPacketLength(command);
            
            if (device.MessageControl.UseCrc)
            {
                AddCrc(command);
            }
            else
            {
                AddChecksum(command);
            }

            CustomCommandUpdate(command);

            return command;
        }

        private byte[] BuildHeader(Device device)
        {
            const int startOfMessageLength = 5;

            var securityControlBlock = device.MessageControl.HasSecurityControlBlock
                ? SecurityControlBlock()
                : ReadOnlySpan<byte>.Empty;
            
            {
                int totalLength = startOfMessageLength + securityControlBlock.Length + 1;
                var pool = MemoryPool<byte>.Shared;
                var buffer = pool.Rent(totalLength);

                try
                {
                    var cursor = buffer.Memory.Span;
                    cursor[0] = StartOfMessage;
                    cursor[1] = Address;
                    cursor[4] = device.MessageControl.ControlByte;
                    cursor = cursor.Slice(startOfMessageLength);

                    securityControlBlock.CopyTo(cursor);
                    cursor = cursor.Slice(securityControlBlock.Length);

                    cursor[0] = CommandCode;

                    return buffer.Memory.Slice(0, totalLength).ToArray();
                }
                finally
                {
                    pool.Dispose();
                }
            }
        }
    }
}