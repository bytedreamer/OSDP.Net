namespace OSDP.Net.Messages
{
    public enum ReplyType
    {
        Ack = 0x40,
        Nak = 0x41,
        PdIdReport = 0x45,
        PdCapabilitiesReport = 0x46,
        LocalStatusReport = 0x48,
        InputStatusReport = 0x49,
        OutputStatusReport = 0x4A,
        ReaderStatusReport = 0x4B,
        RawReaderData = 0x50,
        FormattedReaderData = 0x51,
        KeypadData = 0x53,
        PdCommunicationsConfigurationReport = 0x54,
        BiometricData = 0x57,
        BiometricMatchResult = 0x58,
        CrypticData = 0x76,
        InitialRMac = 0x78,
        Busy = 0x79,
        ManufactureSpecific = 0x90,
        ExtendedRead = 0xB1
    }
}