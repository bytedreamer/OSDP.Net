namespace OSDP.Net.Model.CommandData
{
    public enum OutputControlCode
    {
        /// <summary>NOP – do not alter this output.</summary>
        Nop = 0x00,

        /// <summary>Set the permanent state to OFF, abort timed operation (if any).</summary>
        PermanentStateOffAbortTimedOperation = 0x01,

        /// <summary>Set the permanent state to ON, abort timed operation (if any).</summary>
        PermanentStateOnAbortTimedOperation = 0x02,

        /// <summary>Set the permanent state to OFF, allow timed operation to complete.</summary>
        PermanentStateOffAllowTimedOperation = 0x03,

        /// <summary>Set the permanent state to ON, allow timed operation to complete.</summary>
        PermanentStateOnAllowTimedOperation = 0x04,

        /// <summary>Set the temporary state to ON, resume permanent state on timeout.</summary>
        TemporaryStateOnResumePermanentState = 0x05,

        /// <summary>Set the temporary state to OFF, resume permanent state on timeout.</summary>
        TemporaryStateOffResumePermanentState = 0x06
    }
}