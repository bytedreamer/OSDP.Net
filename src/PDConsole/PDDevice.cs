using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OSDP.Net;
using OSDP.Net.Model;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Model.ReplyData;
using PDConsole.Configuration;
using CommunicationConfiguration = OSDP.Net.Model.CommandData.CommunicationConfiguration;

namespace PDConsole
{
    public class PDDevice : Device
    {
        private readonly DeviceSettings _settings;
        private readonly List<CommandEvent> _commandHistory = new();
        private bool _simulateTamper;
        
        public event EventHandler<CommandEvent> CommandReceived;
        
        public PDDevice(DeviceConfiguration config, DeviceSettings settings, ILoggerFactory loggerFactory)
            : base(config, loggerFactory)
        {
            _settings = settings;
        }
        
        public IReadOnlyList<CommandEvent> CommandHistory => _commandHistory;
        
        public bool SimulateTamper
        {
            get => _simulateTamper;
            set => _simulateTamper = value;
        }
        
        protected override PayloadData HandleIdReport()
        {
            LogCommand("ID Report");
            
            var vendorCode = ConvertHexStringToBytes(_settings.VendorCode, 3);
            return new DeviceIdentification(
                vendorCode,
                (byte)_settings.Model[0],
                _settings.FirmwareMajor,
                _settings.FirmwareMinor,
                _settings.FirmwareBuild,
                (byte)ConvertStringToBytes(_settings.SerialNumber, 4),
                _settings.FirmwareBuild);
        }
        
        protected override PayloadData HandleDeviceCapabilities()
        {
            LogCommand("Device Capabilities");
            return new DeviceCapabilities(_settings.Capabilities.ToArray());
        }
        
        protected override PayloadData HandleCommunicationSet(CommunicationConfiguration commandPayload)
        {
            LogCommand($"Communication Set - Address: {commandPayload.Address}, Baud: {commandPayload.BaudRate}");
            
            return new OSDP.Net.Model.ReplyData.CommunicationConfiguration(
                commandPayload.Address,
                commandPayload.BaudRate);
        }
        
        protected override PayloadData HandleKeySettings(EncryptionKeyConfiguration commandPayload)
        {
            LogCommand($"Key Settings - Type: {commandPayload.KeyType}, Length: {commandPayload.KeyData.Length}");
            return new Ack();
        }
        
        // Override other handlers to just return ACK or NAK
        protected override PayloadData HandleLocalStatusReport()
        {
            LogCommand("Local Status Report");
            return new Ack(); // Simplified - just return ACK
        }
        
        protected override PayloadData HandleInputStatusReport()
        {
            LogCommand("Input Status Report");
            return new Ack(); // Simplified - just return ACK
        }
        
        protected override PayloadData HandleOutputStatusReport()
        {
            LogCommand("Output Status Report");
            return new Ack(); // Simplified - just return ACK
        }
        
        protected override PayloadData HandleReaderStatusReport()
        {
            LogCommand("Reader Status Report");
            return new Ack(); // Simplified - just return ACK
        }
        
        protected override PayloadData HandleReaderLEDControl(ReaderLedControls commandPayload)
        {
            LogCommand($"LED Control - Received command");
            return new Ack();
        }
        
        protected override PayloadData HandleBuzzerControl(ReaderBuzzerControl commandPayload)
        {
            LogCommand($"Buzzer Control - Tone: {commandPayload.ToneCode}");
            return new Ack();
        }
        
        protected override PayloadData HandleTextOutput(ReaderTextOutput commandPayload)
        {
            LogCommand($"Text Output - Row: {commandPayload.Row}, Col: {commandPayload.Column}");
            return new Ack();
        }
        
        protected override PayloadData HandleOutputControl(OutputControls commandPayload)
        {
            LogCommand($"Output Control - Received command");
            return new Ack();
        }
        
        protected override PayloadData HandleBiometricRead(BiometricReadData commandPayload)
        {
            LogCommand($"Biometric Read - Received command");
            return new Nak(ErrorCode.UnableToProcessCommand);
        }
        
        protected override PayloadData HandleManufacturerCommand(OSDP.Net.Model.CommandData.ManufacturerSpecific commandPayload)
        {
            LogCommand($"Manufacturer Specific - Vendor: {BitConverter.ToString(commandPayload.VendorCode)}");
            return new Ack();
        }
        
        protected override PayloadData HandlePivData(GetPIVData commandPayload)
        {
            LogCommand($"Get PIV Data - Received command");
            return new Nak(ErrorCode.UnableToProcessCommand);
        }
        
        protected override PayloadData HandleAbortRequest()
        {
            LogCommand("Abort Request");
            return new Ack();
        }
        
        // Method to send simulated card read
        public void SendSimulatedCardRead(string cardData)
        {
            if (!string.IsNullOrEmpty(cardData))
            {
                try
                {
                    var cardBytes = ConvertHexStringToBytes(cardData, cardData.Length / 2);
                    var bitArray = new BitArray(cardBytes);
                    
                    // Enqueue the card data reply for the next poll
                    EnqueuePollReply(new RawCardData(0, FormatCode.NotSpecified, bitArray));
                    LogCommand($"Simulated card read: {cardData}");
                }
                catch (Exception ex)
                {
                    LogCommand($"Error simulating card read: {ex.Message}");
                }
            }
        }
        
        // Method to simulate keypad entry (using formatted card data as workaround)
        public void SimulateKeypadEntry(string keys)
        {
            if (!string.IsNullOrEmpty(keys))
            {
                try
                {
                    // Note: KeypadData doesn't inherit from PayloadData, so we use FormattedCardData as workaround
                    EnqueuePollReply(new FormattedCardData(0, ReadDirection.Forward, keys));
                    LogCommand($"Simulated keypad entry: {keys}");
                }
                catch (Exception ex)
                {
                    LogCommand($"Error simulating keypad entry: {ex.Message}");
                }
            }
        }
        
        private void LogCommand(string commandDescription)
        {
            var commandEvent = new CommandEvent
            {
                Timestamp = DateTime.Now,
                Description = commandDescription
            };
            
            _commandHistory.Add(commandEvent);
            if (_commandHistory.Count > 100) // Keep only last 100 commands
            {
                _commandHistory.RemoveAt(0);
            }
            
            CommandReceived?.Invoke(this, commandEvent);
        }
        
        private static byte[] ConvertHexStringToBytes(string hex, int expectedLength)
        {
            hex = hex.Replace(" ", "").Replace("-", "");
            var bytes = new byte[expectedLength];
            
            for (int i = 0; i < Math.Min(hex.Length / 2, expectedLength); i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            
            return bytes;
        }
        
        private static uint ConvertStringToBytes(string str, int byteCount)
        {
            uint result = 0;
            for (int i = 0; i < Math.Min(str.Length, byteCount); i++)
            {
                result = (result << 8) | str[i];
            }
            return result;
        }
    }
    
    public class CommandEvent
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
    }
}