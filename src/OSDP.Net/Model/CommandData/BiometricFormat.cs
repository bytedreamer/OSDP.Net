namespace OSDP.Net.Model.CommandData;

/// <summary>
/// Format of data to be scanned.
/// </summary>
public enum BiometricFormat
{
    /// <summary>Default method to scan.</summary>
    NotSpecified = 0x00,
    /// <summary>Send raw fingerprint data as PGM.</summary>
    RawFingerprintData = 0x01,
    /// <summary>ANSI/INCITS 378 fingerprint template.</summary>
    FingerPrintTemplate = 0x02
}