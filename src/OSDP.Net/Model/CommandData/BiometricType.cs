namespace OSDP.Net.Model.CommandData;

/// <summary>
/// The body part that is to be scanned.
/// </summary>
public enum BiometricType
{
#pragma warning disable CS1591
    NotSpecified = 0x00,
    RightThumbPrint = 0x01,
    RightIndexFingerPrint = 0x02,
    RightMiddleFingerPrint = 0x03,
    RightRingFingerPrint = 0x04,
    RightLittleFingerPrint = 0x05,
    LeftThumbPrint = 0x06,
    LeftIndexFingerPrint = 0x07,
    LeftMiddleFingerPrint = 0x08,
    LeftRingFingerPrint = 0x09,
    LeftLittleFingerPrint = 0x0A,
    RightIrisScan = 0x0B,
    RightRetinaScan = 0x0C,
    LeftIrisScan = 0x0D,
    LeftRetinaScan = 0x0E,
    FullFaceImage = 0x0F,
    RightHandGeometry = 0x10,
    LeftHandGeometry = 0x11
#pragma warning restore CS1591
}