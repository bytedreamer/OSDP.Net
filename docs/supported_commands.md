# Supported Commands #

## Commands ##
| Name | Value | Support |
|:-----|:------|:-------:|
| osdp_POLL     | 0x60 | YES |
| osdp_ID       | 0x61 | YES |
| osdp_CAP      | 0x62 | YES |
| osdp_DIAG     | 0x63 | NO |
| osdp_LSTAT    | 0x64 | YES |
| osdp_ISTAT    | 0x65 | YES |
| osdp_OSTAT    | 0x66 | YES |
| osdp_RSTAT    | 0x67 | YES |
| osdp_OUT      | 0x68 | YES |
| osdp_LED      | 0x69 | YES |
| osdp_BUZ      | 0x6A | YES |
| osdp_TEXT     | 0x6B | YES |
| osdp_RMODE    | 0x6C | REMOVED |
| osdp_TDSET    | 0x6D | REMOVED | 
| osdp_COMSET   | 0x6E | NO | 
| osdp_DATA     | 0x6F | REMOVED |
| osdp_XMIT     | 0x70 | REMOVED |
| osdp_PROMPT   | 0x71 | NO |
| osdp_SPE      | 0x72 | REMOVED |
| osdp_BIOREAD  | 0x73 | NO |
| osdp_BIOMATCH | 0x74 | NO |
| osdp_KEYSET   | 0x75 | NO |
| osdp_CHLNG    | 0x76 | YES |
| osdp_SCRYPT   | 0x77 | YES |
| osdp_CONT     | 0x79 | REMOVED |
| osdp_ABORT    | 0x7A | NO |
| osdp_MAXREPLY | 0x7B | NO |
| osdp_MFG      | 0x80 | NO |

## Replies ##
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
| osdp_COM      | 0x54 | NO  |
| osdp_SCREP    | 0x55 | REMOVED |
| osdp_SPER     | 0x56 | REMOVED |
| osdp_BIOREADR | 0x57 | NO  |
| osdp_BIOMATCHR  | 0x58 | NO |
| osdp_CCRYPT   | 0x76 | YES |
| osdp_RMAC_I   | 0x78 | YES |
| osdp_BUSY     | 0x79 | NO  |
| osdp_MFGREP   | 0x90 | NO  |
| osdp_XRD      | 0xB1 | NO  |
