# Installing Busylight Shift Light for SimHub

## Requirements

- Windows with [SimHub](https://www.simhubdash.com/download-2/) installed
- A supported kuando Busylight USB device
- The official Plenom Busylight SDK 4.0.7 runtime

SimHub plugins are loaded from the SimHub installation directory. The usual location is:

```text
C:\Program Files (x86)\SimHub\
```

## Install

1. Close SimHub.
2. Download `BusylightShiftLight-v1.0.0.zip` from this repository's Releases page and extract it.
3. If Windows shows an **Unblock** checkbox in the ZIP or DLL file properties, select it before copying the file.
4. Copy `BusylightShiftLight.dll` into the SimHub installation directory.
5. Obtain `BusylightSDK.dll` directly from the official [Plenom BusylightSDK 4.0.7 NuGet package](https://www.nuget.org/packages/com.plenom.BusylightSDK/4.0.7):
   - Choose **Download package** on the NuGet page.
   - Open the downloaded `.nupkg` as a ZIP archive (rename it to `.zip` if necessary).
   - Extract `lib\net40\BusylightSDK.dll`.
   - Copy `BusylightSDK.dll` into the same SimHub installation directory.
6. Start SimHub.
7. Open **Settings > Plugins** (called **Add/Remove Features** in some SimHub versions), find **Busylight Shift Indicator**, and enable it if it is not already enabled.
8. Open the plugin from SimHub's left sidebar to adjust the warning threshold, colors, and flash interval.

Do not copy the SimHub assemblies from a build output or another computer. SimHub supplies its own compatible copies of `SimHub.Plugins.dll`, `GameReaderCommon.dll`, `SimHub.Logging.dll`, and `log4net.dll`.

## Upgrade

1. Close SimHub.
2. Replace the existing `BusylightShiftLight.dll` with the newer release DLL.
3. Start SimHub. Existing plugin settings should be preserved by SimHub.

## Uninstall

1. Close SimHub.
2. Delete `BusylightShiftLight.dll` from the SimHub installation directory.
3. Delete `BusylightSDK.dll` only if no other installed plugin uses it.
4. Start SimHub.

## Troubleshooting

- **Plugin is missing:** confirm the DLLs are directly in the SimHub installation directory, not in a nested folder, and check whether Windows blocked the downloaded files.
- **Plugin fails to load:** confirm `BusylightSDK.dll` version 4.0.7 is beside the plugin DLL.
- **Light is not responding:** reconnect the USB device, try another USB port, and restart SimHub.
- **More detail:** inspect `Logs\SimHub.txt` inside the SimHub installation directory for messages containing `Busylight`.

The plugin is an independent community project and is not affiliated with SimHub, Plenom, or kuando.
