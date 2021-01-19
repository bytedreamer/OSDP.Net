# Supported OSDP v2.2 Commands and Reply Codes

## Commands
| Name | Value | Support | Description | Documentation |
|:-----|:------|:-------:|:------------|:--------------|
| osdp_POLL         | 0x60 | Yes | Poll | None |
| osdp_ID           | 0x61 | Yes | ID Report Request | [IdReport](https://bytedreamer.github.io/OSDP.Net/html/html/M-OSDP.Net.ControlPanel.IdReport.htm) |
| osdp_CAP          | 0x62 | Yes | PD Capabilities Request | [DeviceCapabilities](https://bytedreamer.github.io/OSDP.Net/html/html/M-OSDP.Net.ControlPanel.DeviceCapabilities.htm) |
| osdp_LSTAT        | 0x64 | Yes | Local Status Report Request | [LocalStatus](https://bytedreamer.github.io/OSDP.Net/html/html/M-OSDP.Net.ControlPanel.LocalStatus.htm) |
| osdp_ISTAT        | 0x65 | Yes | Input Status Report Request | [InputStatus](https://bytedreamer.github.io/OSDP.Net/html/html/M-OSDP.Net.ControlPanel.InputStatus.htm) |
| osdp_OSTAT        | 0x66 | Yes | Output Status Report Request | [OutputStatus](https://bytedreamer.github.io/OSDP.Net/html/html/M-OSDP.Net.ControlPanel.OutputStatus.htm) |
| osdp_RSTAT        | 0x67 | Yes | Reader Status Report Request | [ReaderStatus](https://bytedreamer.github.io/OSDP.Net/html/html/M-OSDP.Net.ControlPanel.ReaderStatus.htm) |
| osdp_OUT          | 0x68 | Yes | Output Control Command | [OutputControl](https://bytedreamer.github.io/OSDP.Net/html/html/M-OSDP.Net.ControlPanel.OutputControl.htm) |
| osdp_LED          | 0x69 | Yes | Reader Led Control Command | | 
| osdp_BUZ          | 0x6A | Yes | Reader Buzzer Control Command | |
| osdp_TEXT         | 0x6B | Yes | Text Output Command | |
| osdp_COMSET       | 0x6E | Yes | PD Communication Configuration Command | |
| osdp_BIOREAD      | 0x73 | No  | Scan and Send Biometric Data | |
| osdp_BIOMATCH     | 0x74 | No  | Scan and Match Biometric Template | |
| osdp_KEYSET       | 0x75 | Yes | Encryption Key Set Command | |
| osdp_CHLNG        | 0x76 | Yes | Challenge and Secure Session Initialization Rq. | None |
| osdp_SCRYPT       | 0x77 | Yes | Server Cryptogram | None |
| osdp_ACURXSIZE    | 0x7B | No  | Max ACU receive size | |
| osdp_FILETRANSFER | 0x7C | No  | Send data file to PD | |
| osdp_MFG          | 0x80 | Yes | Manufacturer Specific Command | |
| osdp_XWR          | 0XA1 | Yes | Extended write data | |
| osdp_ABORT        | 0XA2 | No  | Abort PD operation | |
| osdp_PIVDATA      | 0XA3 | Yes | Get PIV Data | [GetPIVData ](https://bytedreamer.github.io/OSDP.Net/html/html/M-OSDP.Net.ControlPanel.GetPIVData.htm) |
| osdp_GENAUTH      | 0XA4 | No  | Request Authenticate | |
| osdp_CRAUTH       | 0XA5 | No  | Request Crypto Response | |
| osdp_KEEPACTIVE   | 0XA7 | No  | PD read activation | |

## Replies
| Name | Value | Support |
|:-----|:------|:-------:|
| osdp_ACK      | 0x40 | YES |
| osdp_NAK      | 0x41 | YES |
| osdp_PDID     | 0x45 | YES |
| osdp_PDCAP    | 0x46 | YES |
| osdp_LSTATR   | 0x48 | YES |
| osdp_ISTATR   | 0x49 | YES |
| osdp_OSTATR   | 0x4A | YES |
| osdp_RSTATR   | 0x4B | YES |
| osdp_RAW      | 0x50 | YES |
| osdp_FMT      | 0x51 | NO  |
| osdp_PRES     | 0x52 | REMOVED |
| osdp_KEYPPAD  | 0x53 | NO  |
| osdp_COM      | 0x54 | YES |
| osdp_SCREP    | 0x55 | REMOVED |
| osdp_SPER     | 0x56 | REMOVED |
| osdp_BIOREADR | 0x57 | NO  |
| osdp_BIOMATCHR  | 0x58 | NO |
| osdp_CCRYPT   | 0x76 | YES |
| osdp_RMAC_I   | 0x78 | YES |
| osdp_BUSY     | 0x79 | NO  |
| osdp_PIVDATAR | 0x80 | YES |
| osdp_MFGREP   | 0x90 | YES  |
| osdp_XRD      | 0xB1 | YES  |
