using System;

namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// Definitions of the capability function codes.
    /// </summary>
    public enum CapabilityFunction
    {
        /// <summary>Function is not known.</summary>
        Unknown = 0,

        /// <summary>
        ///   <para>The compliance level of contact status monitoring.</para>
        ///   <para></para>
        ///   <para>Compliance Levels:</para>
        ///   <list type="number">
        ///     <item>PD monitors and reports the state of the circuit without any supervision. The PD encodes the circuit status per its default interpretation of contact state to active/inactive status.</item>
        ///     <item>Like 1, plus: The PD accepts configuration of the encoding of the open/closed circuit status to the reported active/inactive status. (User may configure each circuit as "normally closed" or "normally open".)</item>
        ///     <item>Like 2, plus: PD supports supervised monitoring. The operating mode for each circuit is determined by configuration settings.</item>
        ///     <item>Like 3, plus: the PD supports custom End-Of-Line settings within the Manufacturer's guidelines.</item>
        ///   </list>
        ///   <para>Number Of: The number of Inputs.</para>
        /// </summary>
        ContactStatusMonitoring = 1,

        /// <summary>
        ///   <para>The compliance level of output control.</para>
        ///   <para></para>
        ///   <para>Compliance Levels:</para>
        ///   <list type="number">
        ///     <item>The PD is able to activate and deactivate the Output per direct command from the ACU.</item>
        ///     <item>Like 1, plus: The PD is able to accept configuration of the Output driver to set the inactive state of the Output. The typical state of an inactive Output is the state of the Output when no power is applied to the PD and the Output device (relay) is not energized. The inverted drive setting causes the PD to energize the Output during the inactive state and de- energize the Output during the active state.</item>
        ///     <item>Like 2, plus: PD supports supervised monitoring. The operating mode for each circuit is determined by configuration settings.</item>
        ///     <item>Like 3, plus: the PD supports custom End-Of-Line settings within the Manufacturer's guidelines.</item>
        ///   </list>
        ///   <para>Number Of: The number of Outputs.</para>
        /// </summary>
        OutputControl = 2,

        /// <summary>
        ///   <para>The form card data format presented to the control panel.</para>.
        ///   <para></para>
        ///   <para>Compliance Levels:</para>
        ///   <list type="number">
        ///     <item>The PD sends card data to the ACU as array of bits, not exceeding 1024 bits.</item>
        ///     <item>The PD sends card data to the ACU as array of BCD characters, not exceeding 256 characters.</item>
        ///     <item>The PD can send card data to the ACU as array of bits, or as an array of BCD characters.</item>
        ///   </list>
        ///   <para>Number Of: N/A, set to 0.</para>
        /// </summary>
        CardDataFormat = 3,

        /// <summary>
        ///   <para>The compliance level of reader LEDs.</para>
        ///   <para></para>
        ///   <para>Compliance Levels:</para>
        ///   <list type="number">
        ///     <item>The PD support on/off control only.</item>
        ///     <item>The PD supports timed commands.</item>
        ///     <item>Like 2, plus bi-color LEDs.</item>
        ///     <item>Like 2, plus tri-color LEDs.</item>
        ///   </list>
        ///   <para>Number Of: The number of LEDs per reader.</para>
        /// </summary>
        ReaderLEDControl = 4,

        /// <summary>
        ///   <para>The compliance level of reader audio output.</para>
        ///   <para></para>
        ///   <para>Compliance Levels:</para>
        ///   <list type="number">
        ///     <item>The PD supports on/off control only.</item>
        ///     <item>The PD supports timed commands.</item>
        ///   </list>
        ///   <para>Number Of: This field is ignored.</para>
        /// </summary>
        ReaderAudibleOutput = 5,

        /// <summary>
        ///   <para>The compliance level of reader text output.</para>
        ///   <para></para>
        ///   <para>Compliance Levels:</para>
        ///   <list type="number">
        ///     <item>The PD has no text display support.</item>
        ///     <item>The PD supports 1 row of 16 characters.</item>
        ///     <item>The PD supports 2 rows of 16 characters.</item>
        ///     <item>The PD supports 4 rows of 16 characters.</item>
        ///   </list>
        ///   <para>Number Of: Number of textual displays per reader.</para>
        /// </summary>
        ReaderTextOutput = 6,

        [Obsolete("This capability function is obsolete.", false)]
        TimeKeeping = 7,

        /// <summary>
        ///   <para>Check character support for the PD.</para>
        ///   <para></para>
        ///   <para>Compliance Levels:</para>
        ///   <list type="number">
        ///     <item>The PD does not support CRC-16, only checksum mode.</item>
        ///     <item>The PD supports the 16-bit CRC-16 mode.</item>
        ///   </list>
        ///   <para>Number Of: N/A, set to 0.</para>
        /// </summary>
        CheckCharacterSupport = 8,

        /// <summary>
        ///   <para>Secure communications support for the PD.</para>
        ///   <para></para>
        ///   <para>Compliance Levels:</para>
        ///   <para>Bit 0 (mask 0x01) – AES128 support.</para>
        ///   <para></para>
        ///   <para>Number Of: (Bit-0) default AES128 key.</para>
        /// </summary>
        CommunicationSecurity = 9,

        /// <summary>
        ///   <para>Maximum size single message the PD can receive.</para>
        ///   <para></para>
        ///   <para>Compliance Levels: LSB of the buffer size.</para>
        ///   <para></para>
        ///   <para>Number Of: MSB of the buffer size.</para>
        /// </summary>
        ReceiveBufferSize = 10,

        /// <summary>
        ///   <para>Maximum size multi-part message  the PD can handle.</para>
        ///   <para></para>
        ///   <para>Compliance Levels: LSB of the combined buffer size.</para>
        ///   <para></para>
        ///   <para>Number Of: MSB of the combined buffer size.</para>
        /// </summary>
        LargestCombinedMessageSize = 11,

        /// <summary>
        ///   <para>Smart card communication support for the PD.</para>
        ///   <para></para>
        ///   <para>Compliance Levels:</para>
        ///   <para>Bit 0 (mask 0x01) – PD supports transparent reader mode.</para>
        ///   <para>Bit 1 (mask 0x02) – PD supports extended packet mode.</para>
        ///   <para></para>
        ///   <para>Number Of: N/A, set to 0.</para>
        /// </summary>
        SmartCardSupport = 12,

        /// <summary>
        ///   <para>Number of readers present on PD.</para>
        ///   <para></para>
        ///   <para>Compliance Levels: Must be zero.</para>
        ///   <para></para>
        ///   <para>Number Of: Indicates the number of attached downstream readers.</para>
        /// </summary>
        Readers = 13,

        /// <summary>
        ///   <para>The compliance level of reader biometric input.</para>
        ///   <para></para>
        ///   <para>Compliance Levels:</para>
        ///   <list type="number">
        ///     <item>No Biometric.</item>
        ///     <item>Fingerprint, Template 1.</item>
        ///     <item>Fingerprint, Template 2.</item>
        ///     <item>Iris, Template 1.</item>
        ///   </list>
        ///   <para>Number Of: N/A, set to 0.</para>
        /// </summary>
        Biometrics = 14,

        /// <summary>
        ///   <para>Secure PIN entry support for the PD.</para>
        ///   <para></para>
        ///   <para>Compliance Levels:</para>
        ///   <list type="number">
        ///     <item>Does not support SPE.</item>
        ///     <item>Supports SPE.</item>
        ///   </list>
        ///   <para>Number Of: N/A, set to 0.</para>
        /// </summary>
        SecurePINEntry = 15,

        /// <summary>
        ///   <para>Version of OSDP support by the PD.</para>
        ///   <para></para>
        ///   <para>Compliance Levels:</para>
        ///   <list type="number">
        ///     <item>Unspecified</item>
        ///     <item>IEC 60839-11-5</item>.
        ///     <item>SIA OSDP 2.2</item>
        ///   </list>
        ///   <para>Number Of: N/A, set to 0.</para>
        /// </summary>
        OSDPVersion = 16
    }
}