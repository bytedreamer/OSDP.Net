using System;
using System.Collections.Generic;
using OSDP.Net.Messages;
using OSDP.Net.Messages.SecureChannel;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Command data to send a request to the PD to perform a biometric scan and return data.
    /// </summary>
    public class BiometricTemplateData : CommandData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BiometricTemplateData"/> class.
        /// </summary>
        /// <param name="readerNumber">The reader number starting at 0.</param>
        /// <param name="biometricType">The type/body part to scan.</param>
        /// <param name="biometricFormatType">The format of the attached template.</param>
        /// <param name="qualityThreshold">The threshold required for accepting biometric match.</param>
        /// <param name="templateData">The biometric template data.</param>
        public BiometricTemplateData(byte readerNumber, BiometricType biometricType, BiometricFormat biometricFormatType,
            byte qualityThreshold, byte[] templateData)
        {
            ReaderNumber = readerNumber;
            BiometricType = biometricType;
            BiometricFormatType = biometricFormatType;
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
        public BiometricType BiometricType { get; }

        /// <summary>
        /// Gets the format of the attached template.
        /// </summary>
        public BiometricFormat BiometricFormatType { get; }

        /// <summary>
        /// Gets the threshold required for accepting biometric match.
        /// </summary>
        public byte QualityThreshold { get; }

        /// <summary>
        ///  Gets the biometric template data.
        /// </summary>
        public byte[] TemplateData { get; }

        /// <inheritdoc />
        public override CommandType CommandType => CommandType.BioMatch;

        /// <inheritdoc />
        public override byte Code => (byte)CommandType;
        
        /// <inheritdoc />
        public override ReadOnlySpan<byte> SecurityControlBlock() => SecurityBlock.CommandMessageWithDataSecurity;

        /// <inheritdoc />
        public override byte[] BuildData()
        {
            var data = new List<byte>
            {
                ReaderNumber,
                (byte)BiometricType,
                (byte)BiometricFormatType,
                QualityThreshold
            };
            data.AddRange(Message.ConvertShortToBytes((ushort)TemplateData.Length));
            data.AddRange(TemplateData);
            return data.ToArray();
        }

        /// <summary>Parses the message payload bytes</summary>
        /// <param name="data">Message payload as bytes</param>
        /// <returns>An instance of BiometricTemplateData representing the message payload</returns>
        public static BiometricTemplateData ParseData(ReadOnlySpan<byte> data)
        {
            short length = Message.ConvertBytesToShort(data.Slice(4, 2), true);
            
            return new BiometricTemplateData(
                data[0],
                (BiometricType)data[1],
                (BiometricFormat)data[2],
                data[3],
                data.Slice(6, length).ToArray());
        }
    }
}