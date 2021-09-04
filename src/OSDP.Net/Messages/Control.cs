namespace OSDP.Net.Messages
{
    public class Control
    {
        private readonly bool _hasSecurityControlBlock;

        public Control(byte sequence, bool useCrc, bool hasSecurityControlBlock)
        {
            Sequence = sequence;
            UseCrc = useCrc;
            _hasSecurityControlBlock = hasSecurityControlBlock;
        }

        public byte Sequence { get; private set; }

        public bool UseCrc { get; }

        public bool HasSecurityControlBlock => !IsSendingMultiMessageNoSecureChannel && _hasSecurityControlBlock;

        public byte ControlByte =>
            (byte) (Sequence & 0x03 | (UseCrc ? 0x04 : 0) | (HasSecurityControlBlock ? 0x08 : 0));

        public bool IsSendingMultiMessageNoSecureChannel { get; set; }

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