using System.Collections.Generic;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Command data to send a request to the PD to perform a biometric scan and match.
    /// </summary>
    public class BiometricTemplateData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BiometricTemplateData"/> class.
        /// </summary>
        /// <param name="readerNumber">The reader number starting at 0.</param>
        /// <param name="type">The type/body part to scan.</param>
        /// <param name="format">The format of the attached template.</param>
        /// <param name="qualityThreshold">The threshold required for accepting biometric match.</param>
        /// <param name="templateData">The biometric template data.</param>
        public BiometricTemplateData(byte readerNumber, BiometricType type, BiometricFormat format,
            byte qualityThreshold, byte[] templateData)
        {
            ReaderNumber = readerNumber;
            Type = type;
            Format = format;
            QualityThreshold = qualityThreshold;
            TemplateData = templateData;
        }

        /// <summary>
        /// Gets the reader number starting at 0.
        /// </summary>
        public byte ReaderNumber { get; }

        /// <summary>
        /// Gets the type/body part to scan.
        /// </summary>
        public BiometricType Type { get; }

        /// <summary>
        /// Gets the format of the attached template.
        /// </summary>
        public BiometricFormat Format { get; }

        /// <summary>
        /// Gets the threshold required for accepting biometric match.
        /// </summary>
        public byte QualityThreshold { get; }

        /// <summary>
        ///  Gets the biometric template data.
        /// </summary>
        public byte[] TemplateData { get; }

        internal IEnumerable<byte> BuildData()
        {
            var data = new List<byte>
            {
                ReaderNumber,
                (byte)Type,
                (byte)Format,
                QualityThreshold
            };
            data.AddRange(Message.ConvertShortToBytes((ushort)TemplateData.Length));
            data.AddRange(TemplateData);
            return data;
        }
    }

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
}