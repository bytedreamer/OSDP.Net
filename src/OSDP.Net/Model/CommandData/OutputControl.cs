namespace OSDP.Net.Model.CommandData
{
    public class OutputControl
    {}

    public enum OutputControlCode
    {
        Nop = 0x00,
        PermanentStateOffAbortTimedOperation = 0x01,
        PermanentStateOnAbortTimedOperation = 0x02,
        PermanentStateOffAllowTimedOperation = 0x03,
        PermanentStateOnAllowTimedOperation = 0x04,
        TemporaryStateOnResumePermanentState = 0x05,
        TemporaryStateOffResumePermanentState = 0x06
    }
}