namespace OSDP.Net.Messages
{
    public enum CommandType : byte
    {
        Poll = 0x60,
        IdReport = 0x61,
        DeviceCap = 0x62,
        LocalStatus = 0x64,
        InputStatus = 0x65,
        OutputStatus = 0x66,
        TamperStatus = 0x67,
        OutputControl = 0x68,
        LEDControl = 0x69,
        BuzzerControl = 0x6a,
        TextOutput = 0x6b,
        CommCfg = 0x6e,
        SendBio = 0x73,
        MatchBio = 0x74,
        KeySet = 0x75,
        SessChlng = 0x76,
        SCrypt = 0x77,
        MaxReplySize = 0x7b,
        FileXfr = 0x7c,
        MfgCmd = 0x80,
        XWrite = 0xa1,
        Abort = 0xa2,
        GetPiv = 0xa3,
        ReqAuth = 0xa4,
        ReqCrypto = 0xa5,
        KeepAlive = 0xa7
    }
}