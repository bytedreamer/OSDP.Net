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

        public byte Sequence { get; private set; }
        public bool UseCrc { get; }
        public bool HasSecurityControlBlock { get; }

        public byte ControlByte =>
            (byte) (Sequence & 0x03 | (UseCrc ? 0x04 : 0) | (HasSecurityControlBlock ? 0x08 : 0));

        public void IncrementSequence(byte sequence)
        {
            sequence++;
            if (sequence > 3)
            {
                sequence = 1;
            }

            Sequence = sequence;
        }

        public void ResetSequence()
        {
            Sequence = 0;
        }
    }
}