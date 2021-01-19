namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// Possible states that the reader is currently reporting.
    /// </summary>
    public enum ReaderTamperStatus
    {
        /// <summary>Reader is in a normal state.</summary>
        Normal = 0x00,

        /// <summary>Reader is in a not connected state.</summary>
        NotConnected = 0x01,

        /// <summary>Reader is in a tamper state.</summary>
        Tamper = 0x02
    }
}