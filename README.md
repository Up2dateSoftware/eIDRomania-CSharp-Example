# eIDRomania Desktop SDK — C# Example

Example application demonstrating how to use the [EIDRomania.SDK](https://www.nuget.org/packages/EIDRomania.SDK) NuGet package to read Romanian Electronic Identity Cards (CEI) via PC/SC smart card readers.

## Prerequisites

- .NET 9.0+
- A PC/SC smart card reader (contact or NFC/contactless)
- A Romanian electronic identity card (CEI)
- A valid eIDRomania Desktop SDK license key (contact: office@up2date.ro)

## Setup

1. Replace `YOUR_LICENSE_KEY_HERE` in `Program.cs` with your license key.
2. Connect a smart card reader to your computer.
3. Place the Romanian eID card on the reader.

## Run

```bash
dotnet run
```

## What it demonstrates

| Feature | Description |
|---------|-------------|
| SDK initialization | `sdk.Initialize(licenseKey, appIdentifier)` |
| Reader listing | `sdk.AvailableReaders` |
| CAN-only read | `sdk.ReadAsync(can)` — MRZ data + face photo |
| CAN+PIN read | `sdk.ReadAsync(can, pin)` — full data + address |
| Progress callback | Real-time progress percentage |
| Error handling | Typed `EIDReadException` with `ErrorCode` enum |
| Authentication | Passive Authentication result (SOD verification) |

## SDK Installation

The SDK is installed from NuGet:

```bash
dotnet add package EIDRomania.SDK
```

## License

Example code: MIT. SDK: Proprietary — Up2Date Software SRL.
