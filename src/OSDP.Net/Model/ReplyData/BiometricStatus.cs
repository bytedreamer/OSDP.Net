namespace OSDP.Net.Model.ReplyData;

/// <summary>
/// Results of the biometric scanning.
/// </summary>
public enum BiometricStatus
{
#pragma warning disable CS1591
    Success = 0x00,
    Timeout = 0x01,
    UnknownError = 0xFF
#pragma warning restore CS1591
}