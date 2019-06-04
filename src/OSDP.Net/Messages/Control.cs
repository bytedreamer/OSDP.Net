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

        private byte Sequence { get; set; }
        public bool UseCrc { get; }
        private bool HasSecurityControlBlock { get; }

        public byte ControlByte =>
            (byte) (Sequence & 0x03 | (UseCrc ? 0x04 : 0) | (HasSecurityControlBlock ? 0x08 : 0));

        public void IncrementSequence()
        {
            Sequence++;
            if (Sequence > 3)
            {
                Sequence = 1;
            }
        }
    }
}