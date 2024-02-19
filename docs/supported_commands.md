# Supported OSDP v2.2 Commands and Reply Codes

## Commands
| Name              | Value | Support | Description                                     | Documentation                                                                                                                               |
|:------------------|:------|:-------:|:------------------------------------------------|:--------------------------------------------------------------------------------------------------------------------------------------------|
| osdp_POLL         | 0x60  |   Yes   | Poll                                            | None                                                                                                                                        |
| osdp_ID           | 0x61  |   Yes   | ID Report Request                               | [IdReport](https://z-bitco.com/downloads/OSDP.Net/docs/class_o_s_d_p_1_1_net_1_1_control_panel.html#a7865680fcbac04393f1a1a7c5c370afd)      |
| osdp_CAP          | 0x62  |   Yes   | PD Capabilities Request                         | [DeviceCapabilities](https://z-bitco.com/downloads/OSDP.Net/docs/class_o_s_d_p_1_1_net_1_1_control_panel.html#a41e7c5b63296041c4e1d72f9f4baa254) |
| osdp_LSTAT        | 0x64  |   Yes   | Local Status Report Request                     | [LocalStatus](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.LocalStatus.htm)                                     |
| osdp_ISTAT        | 0x65  |   Yes   | Input Status Report Request                     | [InputStatus](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.InputStatus.htm)                                     |
| osdp_OSTAT        | 0x66  |   Yes   | Output Status Report Request                    | [OutputStatus](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.OutputStatus.htm)                                   |
| osdp_RSTAT        | 0x67  |   Yes   | Reader Status Report Request                    | [ReaderStatus](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.ReaderStatus.htm)                                   |
| osdp_OUT          | 0x68  |   Yes   | Output Control Command                          | [OutputControl](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.OutputControl.htm)                                 |
| osdp_LED          | 0x69  |   Yes   | Reader Led Control Command                      | [ReaderLedControl](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.ReaderLedControl.htm)                           | 
| osdp_BUZ          | 0x6A  |   Yes   | Reader Buzzer Control Command                   | [ReaderBuzzerControl](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.ReaderBuzzerControl.htm)                     |
| osdp_TEXT         | 0x6B  |   Yes   | Text Output Command                             | [ReaderTextOutput](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.ReaderTextOutput.htm)                           |
| osdp_COMSET       | 0x6E  |   Yes   | PD Communication Configuration Command          | [CommunicationConfiguration](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.CommunicationConfiguration.htm)       |
| osdp_BIOREAD      | 0x73  |   Yes   | Scan and Send Biometric Data                    | [ScanAndSendBiometricData](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.ScanAndSendBiometricData.htm)           |
| osdp_BIOMATCH     | 0x74  |   Yes   | Scan and Match Biometric Template               | [ScanAndMatchBiometricTemplate](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.ScanAndMatchBiometricTemplate.htm) |
| osdp_KEYSET       | 0x75  |   Yes   | Encryption Key Set Command                      | [EncryptionKeySet](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.EncryptionKeySet.htm)                           |
| osdp_CHLNG        | 0x76  |   Yes   | Challenge and Secure Session Initialization Rq. | None                                                                                                                                        |
| osdp_SCRYPT       | 0x77  |   Yes   | Server Cryptogram                               | None                                                                                                                                        |
| osdp_ACURXSIZE    | 0x7B  |   Yes   | Max ACU receive size                            | [ACUReceivedSize](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.ACUReceivedSize.htm)                             |
| osdp_FILETRANSFER | 0x7C  |   Yes   | Send data file to PD                            | [FileTransfer](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.FileTransfer.htm)                                   |
| osdp_MFG          | 0x80  |   Yes   | Manufacturer Specific Command                   | [ManufacturerSpecificCommand](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.ManufacturerSpecificCommand.htm)     |
| osdp_XWR          | 0XA1  |   No    | Extended write data                             |                                                                                                                                             |
| osdp_ABORT        | 0XA2  |   Yes   | Abort PD operation                              | [AbortCurrentOperation](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.AbortCurrentOperation.htm)                 |
| osdp_PIVDATA      | 0XA3  |   Yes   | Get PIV Data                                    | [GetPIVData](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.GetPIVData.htm)                                       |
| osdp_GENAUTH      | 0XA4  |   No    | Request Authenticate                            |                                                                                                                                             |
| osdp_CRAUTH       | 0XA5  |   No    | Request Crypto Response                         |                                                                                                                                             |
| osdp_KEEPACTIVE   | 0XA7  |   Yes   | PD read activation                              | [KeeprReaderActive](https://z-bitco.com/downloads/OSDP.Net/docs/html/M-OSDP.Net.ControlPanel.KeepReaderActive.htm)                          |

## Replies
| Name           | Value | Support | Description                                | Documentation |
|:---------------|:------|:-------:|:-------------------------------------------|:--------------|
| osdp_ACK       | 0x40  |   Yes   | Command accepted, nothing else to report   | None          |
| osdp_NAK       | 0x41  |   Yes   | Command not processed                      |               |
| osdp_PDID      | 0x45  |   Yes   | PD ID Report                               |               |
| osdp_PDCAP     | 0x46  |   Yes   | PD Capabilities Report                     |               |
| osdp_LSTATR    | 0x48  |   Yes   | Local Status Report                        |               |
| osdp_ISTATR    | 0x49  |   Yes   | Input Status Report                        |               |
| osdp_OSTATR    | 0x4A  |   Yes   | Output Status Report                       |               |
| osdp_RSTATR    | 0x4B  |   Yes   | Reader Status Report                       |               |
| osdp_RAW       | 0x50  |   Yes   | Reader Data � Raw bit image of card data   |               |
| osdp_FMT       | 0x51  |   No    | Reader Data � Formatted character stream   |               |
| osdp_KEYPPAD   | 0x53  |   Yes   | Keypad Data                                |               |
| osdp_COM       | 0x54  |   Yes   | PD Communications Configuration Report     |               |
| osdp_BIOREADR  | 0x57  |   Yes   | Biometric Data                             |               |
| osdp_BIOMATCHR | 0x58  |   Yes   | Biometric Match Result                     |               |
| osdp_CCRYPT    | 0x76  |   Yes   | Client's ID, Random Number, and Cryptogram | None          |
| osdp_RMAC_I    | 0x78  |   Yes   | Initial R-MAC                              | None          |
| osdp_BUSY      | 0x79  |   Yes   | PD is Busy reply                           | None          |
| osdp_FTSTAT    | 0x7A  |   No    |                                            |               |
| osdp_PIVDATAR  | 0x80  |   Yes   | PIV Data Reply                             |               |
| osdp_GENAUTHR  | 0x81  |   No    | Authentication response                    |               |
| osdp_CRAUTHR   | 0x82  |   No    | Response to challenge                      |               |
| osdp_MFGSTATR  | 0x83  |   No    | MFG specific status                        |               |
| osdp_MFGERRR   | 0x84  |   No    | MFG specific error                         |               |
| osdp_MFGREP    | 0x90  |   Yes   | Manufacturer Specific Reply                |               |
| osdp_XRD       | 0xB1  |   No    | Extended Read Response                     |               |
