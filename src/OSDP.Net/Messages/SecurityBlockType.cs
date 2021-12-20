namespace OSDP.Net.Messages
{
    internal enum SecurityBlockType
    {
        BeginNewSecureConnectionSequence = 0x11,
        SecureConnectionSequenceStep2 = 0x12,
        SecureConnectionSequenceStep3 = 0x13,
        SecureConnectionSequenceStep4 = 0x14,
        CommandMessageWithNoDataSecurity = 0x15,
        ReplyMessageWithNoDataSecurity = 0x16,
        CommandMessageWithDataSecurity = 0x17,
        ReplyMessageWithDataSecurity = 0x18
    }
}