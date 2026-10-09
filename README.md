# Network Activity Tray v0.1

A tiny Windows 11 tray application inspired by PENetwork Manager's activity indicator.

## Behaviour

- Idle: white triangle
- Send: blue up arrow
- Receive: blue down arrow
- Both: both blue arrows
- Disconnected: red X

The app automatically selects an active non-loopback/non-tunnel interface, preferring one with a default gateway. No speed numbers are shown.

## Build requirements

- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 is optional

## Run from source

```powershell
dotnet run
```

## Build

```powershell
dotnet build -c Release
```

## Publish a portable self-contained 64-bit EXE

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output will be under:

```text
bin\Release\net8.0-windows\win-x64\publish\
```

## Corporate-device note

A freshly built unsigned executable can still be blocked by Defender, SmartScreen, AppLocker, WDAC, or another organizational application-control policy. Do not bypass those controls; use your normal IT approval process if it is blocked.

## v0.1 limitations

- no settings UI
- no manual adapter picker
- no run-at-startup option
- no code signing
- fixed 350 ms polling interval
