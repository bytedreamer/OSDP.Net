using System;
using System.Buffers;
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
                var combined = EncryptData(header, device);

                // include mac and crc in length before generating mac
                ushort macAndChecksumLength = (ushort) (4 + (device.MessageControl.UseCrc ? 2 : 1));
                
                AddPacketLength(combined, macAndChecksumLength);

                var mac = device.GenerateMac(combined, true).Slice(0, 4);

                int commandLength = combined.Length + macAndChecksumLength;
                var pool = MemoryPool<byte>.Shared;
                var buffer = pool.Rent(commandLength);
                try
                {
                    var cursor = buffer.Memory.Span;

                    combined.CopyTo(cursor);
                    cursor = cursor.Slice(combined.Length);

                    mac.CopyTo(cursor);

                    command = buffer.Memory.Slice(0, commandLength).ToArray();
                }
                finally
                {
                    pool.Dispose();
                }
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

        private Span<byte> BuildHeader(Device device)
        {
            const int startOfMessageLength = 5;

            var securityControlBlock = device.MessageControl.HasSecurityControlBlock
                ? SecurityControlBlock()
                : ReadOnlySpan<byte>.Empty;

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

        private Span<byte> EncryptData(ReadOnlySpan<byte> header, Device device)
        {
            var encryptedData = EncryptedData(device);
            
            int totalLength = header.Length + encryptedData.Length ;
            var pool = MemoryPool<byte>.Shared;
            var buffer = pool.Rent(totalLength);

            try
            {
                var cursor = buffer.Memory.Span;
                
                header.CopyTo(cursor);
                cursor = cursor.Slice(header.Length);

                encryptedData.CopyTo(cursor);

                return buffer.Memory.Slice(0, totalLength).ToArray();
            }
            finally
            {
                pool.Dispose();
            }
        }
    }
}