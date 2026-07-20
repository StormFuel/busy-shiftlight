# Busylight Shift Light - C# SimHub Plugin

A native C# SimHub plugin that controls a physical USB Busylight device as an adaptive racing shift light indicator. 

Built from the Python reference implementation, this C# version runs directly inside SimHub for zero-latency telemetry processing.

## Features

* **Zero-Latency Processing**: Runs inside SimHub's telemetry loop for instant responsiveness
* **Adaptive RPM Thresholds**: Uses vehicle-specific shift points from SimHub or falls back to 95% of max RPM
* **Three-State Indicator**:
  - **Off**: RPM below warning threshold
  - **Yellow Solid**: RPM in warning zone (85% of shift RPM)
  - **Red Flashing**: RPM at or above shift point (10Hz flash, 50ms toggle)
* **Smart Suppression**: Light turns off in Neutral, Reverse, or when pit lane limiter is active
* **Configurable Settings**: Adjust thresholds, colors, and flash frequency via SimHub's built-in GUI
* **Device Fallback**: Works in simulation mode if no Busylight device is detected

## Project Structure

```
BusylightShiftLight/
├── BusylightPlugin.cs          # Main plugin class (IDataPlugin, IWPFSettingsV2)
├── BusylightController.cs      # USB device connection & control logic
├── PluginSettings.cs           # Configuration model (auto-serialized by SimHub)
├── SettingsView.xaml           # WPF configuration panel UI
├── SettingsView.xaml.cs        # UI code-behind
├── BusylightShiftLight.csproj  # Project file (.NET Framework 4.8)
├── Properties/AssemblyInfo.cs  # Assembly metadata
└── packages.config             # NuGet dependencies
```

## Building the Plugin

### Prerequisites

1. **Visual Studio 2019+** (or any IDE supporting .NET Framework 4.8)
2. **.NET Framework 4.8** (required for SimHub compatibility)
3. **SimHub installed** at `C:\Program Files (x86)\SimHub`

### Steps

1. Open `BusylightShiftLight.csproj` in Visual Studio
2. Uncomment the SimHub DLL references in the `.csproj` file and update the paths if needed:
   ```xml
   <Reference Include="SimHub.Plugins">
     <HintPath>C:\Program Files (x86)\SimHub\SimHub.Plugins.dll</HintPath>
   </Reference>
   <Reference Include="SimHub.Plugins.WPF">
     <HintPath>C:\Program Files (x86)\SimHub\SimHub.Plugins.WPF.dll</HintPath>
   </Reference>
   ```
3. (Optional) Install HidSharp if you don't have the BusylightSDK:
   ```
   Install-Package HidSharp -Version 2.1.0
   ```
4. Build in **Release** mode: `Ctrl+Shift+B`

### Output

The compiled DLL will be located at:
```
BusylightShiftLight/bin/Release/BusylightShiftLight.dll
```

## Installation in SimHub

1. Copy `BusylightShiftLight.dll` to your SimHub root directory:
   ```
   C:\Program Files (x86)\SimHub\
   ```
   
   Optionally copy any dependency DLLs (e.g., `HidSharp.dll` or `BusylightSDK.dll`) to the same folder

2. Restart SimHub or reload plugins:
   - Go to **Settings** → **Plugins**
   - Find "Busylight Shift Indicator" in the list
   - Toggle **Enabled** to activate

3. The plugin's settings panel will appear in SimHub's left sidebar

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

The plugin supports two methods for connecting to the Busylight:

### Option 1: BusylightSDK (Recommended)
- Download the official SDK from the Busylight vendor
- Copy `BusylightSDK.dll` to the plugin folder
- The plugin will auto-detect and use it

### Option 2: HidSharp
- Install via NuGet: `Install-Package HidSharp`
- Uncomment in `packages.config`
- The plugin includes fallback logic to use HidSharp if SDK is unavailable

## Troubleshooting

### Plugin not loading in SimHub
- Verify the DLL is in `C:\Program Files (x86)\SimHub\`
- Check SimHub's log file for errors
- Ensure .NET Framework 4.8 is installed

### Busylight not responding
- Verify the USB device is connected
- Try different USB ports
- Check SimHub's log: "Busylight connected successfully" should appear on startup
- If not detected, the plugin will run in simulation mode with console feedback

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
   - Supports both BusylightSDK and HidSharp
   - Gracefully handles device disconnection

3. **PluginSettings** (Configuration Model)
   - SimHub auto-serializes to JSON
   - Loaded on Init, saved on End
   - Bound to WPF UI for live updates

## References

- [SimHub Plugin Development](https://wiki.simhubdash.com/)
- [SimHub GitHub Examples](https://github.com/zesinger/SimHub)
- [BusylightSDK Documentation](https://www.busylight.com/)
- [HidSharp GitHub](https://github.com/jcurl/RJCP.DLL.SerialPortStream)

---

**Original Python Implementation**: See `../python_original/` folder for the reference implementation and design specifications.
