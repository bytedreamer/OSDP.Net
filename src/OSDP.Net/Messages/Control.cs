namespace OSDP.Net.Messages
{
    public class Control
    {
        public Control(byte sequence, bool useCrc, bool hasSecurityControlBlock)
        {
            Sequence = sequence;
            UseCrc = useCrc;
            HasSecurityControlBlock = hasSecurityControlBlock;
        }

        private byte Sequence { get; }
        private bool UseCrc { get; }
        private bool HasSecurityControlBlock { get; }

        public byte ControlByte =>
            (byte) (Sequence & 0x03 | (UseCrc ? 0x04 : 0) | (HasSecurityControlBlock ? 0x08 : 0));
    }
}