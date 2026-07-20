# Busylight Shift Light - C# SimHub Plugin

A native C# SimHub plugin that controls a physical USB Busylight device as an adaptive racing shift light indicator. 

Built from the Python reference implementation, this C# version runs directly inside SimHub for zero-latency telemetry processing.

## Download and Install

Download the versioned ZIP from this repository's **Releases** page. The archive contains the compiled plugin, its MIT license, and installation instructions. See [INSTALL.md](INSTALL.md) for the complete procedure and the required official Plenom SDK runtime.

The repository also contains a checked-in release archive under `release/` so a usable build remains available with the source.

## Tested Hardware

This plugin was developed and physically tested with the **PLENOM kuando Busylight Omega (USB)**. The hardware smoke test successfully detected the device and exercised its color and off states through the official Busylight SDK.

[Buy the kuando Busylight Omega on Amazon](https://amzn.to/4vDUzso)

**Affiliate disclosure:** The Amazon link above is an affiliate link. I receive a small commission from qualifying purchases made through it, at no additional cost to you.

## Features

* **Zero-Latency Processing**: Runs inside SimHub's telemetry loop for instant responsiveness
* **Adaptive RPM Thresholds**: Uses vehicle-specific shift points from SimHub or falls back to 95% of max RPM
* **Three-State Indicator**:
  - **Off**: RPM below warning threshold
  - **Yellow Solid**: RPM in warning zone (85% of shift RPM)
  - **Red Flashing**: RPM at or above shift point (10Hz flash, 50ms toggle)
* **Smart Suppression**: Light turns off in Neutral, Reverse, or when pit lane limiter is active
* **Configurable Settings**: Adjust thresholds, colors, and flash frequency via SimHub's built-in GUI
* **Hot-plug Recovery**: Retries device discovery when a Busylight is disconnected or connected later

## Project Structure

```
BusylightShiftLight/
├── BusylightPlugin.cs          # Main plugin class (IDataPlugin, IWPFSettingsV2)
├── BusylightController.cs      # USB device connection & control logic
├── PluginSettings.cs           # Configuration model (auto-serialized by SimHub)
├── SettingsView.xaml           # WPF configuration panel UI
├── SettingsView.xaml.cs        # UI code-behind
├── ShiftLightLogic.cs          # Testable telemetry and flashing state machine
├── Tests/                      # Logic tests and physical-device smoke test
├── BusylightShiftLight.csproj  # Project file (.NET Framework 4.8)
├── Properties/AssemblyInfo.cs  # Assembly metadata
```

## Building the Plugin

### Prerequisites

1. **Visual Studio 2019+** (or any IDE supporting .NET Framework 4.8)
2. **.NET Framework 4.8** (required for SimHub compatibility)
3. **SimHub installed** at `C:\Program Files (x86)\SimHub`

### Steps

1. Verify the SimHub reference paths in `BusylightShiftLight.csproj` if SimHub is installed elsewhere.
2. Open `BusylightShiftLight.csproj` in Visual Studio and allow NuGet restore to complete.
3. Build in **Release** mode with Visual Studio/MSBuild. The project uses the official Plenom Busylight SDK package.

The project follows SimHub's SDK convention by reading the `SIMHUB_INSTALL_PATH` environment variable for host assembly references, with `C:\Program Files (x86)\SimHub\` as a fallback.

### Output

The compiled DLL will be located at:
```
BusylightShiftLight/bin/Release/BusylightShiftLight.dll
```

To build, test, and create a versioned release archive:

```powershell
.\scripts\package-release.ps1 -Version 1.0.0
```

## Installation in SimHub

Follow [INSTALL.md](INSTALL.md). In short: close SimHub, copy the release DLL and the official `BusylightSDK.dll` runtime into the SimHub installation directory, restart SimHub, and enable **Busylight Shift Indicator** in the plugin list.

The Plenom runtime is not redistributed in this repository because it has its own license. Users obtain it directly from the official NuGet package linked in the installation guide.

## Configuration

All settings are adjustable via the WPF settings panel in SimHub:

| Setting | Default | Range | Notes |
|---------|---------|-------|-------|
| Fallback Shift RPM | 95% | 80-100% | Used if car doesn't provide shift point |
| Yellow Warning | 85% | 70-95% | Percentage of shift RPM to trigger yellow |
| Flash Interval | 50ms | 20-200ms | Red warning flash on/off toggle |
| Yellow Color | #FFA500 | Hex | Customizable RGB color |
| Red Color | #FF0000 | Hex | Customizable RGB color |

Settings are saved automatically when changed and persist across SimHub restarts.

## USB Device Support

The plugin uses Plenom's official `com.plenom.BusylightSDK` package. The SDK discovers supported kuando hardware, sends RGB commands, and reports device connection changes. Both `BusylightShiftLight.dll` and `BusylightSDK.dll` are required at deployment.

## Troubleshooting

### Plugin not loading in SimHub
- Verify the DLL is in `C:\Program Files (x86)\SimHub\`
- Check SimHub's log file for errors
- Ensure .NET Framework 4.8 is installed

### Busylight not responding
- Verify the USB device is connected
- Try different USB ports
- Check SimHub's log: "Busylight connected successfully" should appear on startup
- If not detected, the plugin remains active and retries discovery when an output command is needed

### Settings not saving
- Ensure you have write permissions to the SimHub folder
- Try restarting SimHub

## Architecture Notes

The plugin architecture is split into three concerns:

1. **BusylightPlugin** (IDataPlugin)
   - Hooks into SimHub's telemetry loop (`DataUpdate`)
   - Implements state machine logic
   - Manages lifecycle (Init, End)

2. **BusylightController** (Hardware Abstraction)
   - Encapsulates device connection logic
   - Uses the official Plenom Busylight SDK and handles reconnects
   - Gracefully handles device disconnection

3. **PluginSettings** (Configuration Model)
   - SimHub auto-serializes to JSON
   - Loaded on Init, saved on End
   - Bound to WPF UI for live updates

## References

- [SimHub Plugin Development](https://wiki.simhubdash.com/)
- [SimHub GitHub Examples](https://github.com/zesinger/SimHub)
- [BusylightSDK Documentation](https://www.busylight.com/)
- [Official Plenom Busylight SDK package](https://www.nuget.org/packages/com.plenom.BusylightSDK)

---

**Original Python Implementation**: See `../python_original/` folder for the reference implementation and design specifications.
