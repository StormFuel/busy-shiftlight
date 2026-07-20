# SimHub Busylight Shift Light - Complete Project

A native C# SimHub plugin for controlling a USB Busylight device as an adaptive racing shift light indicator.

## 📁 Project Structure

```
busy_shiftlight/
├── BusylightShiftLight/           # Main C# plugin project (ACTIVE)
│   ├── BusylightPlugin.cs         # Main plugin class
│   ├── BusylightController.cs     # USB device control
│   ├── PluginSettings.cs          # Configuration model
│   ├── SettingsView.xaml/.cs      # WPF settings UI
│   ├── BusylightShiftLight.csproj # Project file
│   ├── Properties/
│   │   └── AssemblyInfo.cs
│   ├── packages.config
│   └── README.md                  # Plugin documentation
│
├── python_original/               # Reference implementation (ARCHIVED)
│   ├── busy_shiftlight.py
│   ├── test_busy_shiftlight.py
│   ├── config.json
│   └── README.md
│
├── csharp_plugin_design.md        # Architecture & design blueprint
├── requirements.md                # Original requirements spec
└── README.md (this file)
```

## 🚀 Quick Start

### For Development/Building

1. Open `BusylightShiftLight/BusylightShiftLight.csproj` in Visual Studio 2019+
2. Uncomment SimHub DLL references in the `.csproj` file
3. Build in Release mode
4. See [BusylightShiftLight/README.md](BusylightShiftLight/README.md) for detailed build instructions

### For Installation

1. Copy compiled `BusylightShiftLight.dll` to `C:\Program Files (x86)\SimHub\`
2. Restart SimHub
3. Go to **Settings → Plugins** and enable "Busylight Shift Indicator"
4. Configure via the settings panel in SimHub's sidebar

## 📊 Features

✅ **Zero-Latency**: Runs inside SimHub's game loop  
✅ **Adaptive Thresholds**: Uses vehicle-specific shift points  
✅ **Three States**: Off → Yellow (warning) → Red (flashing redline)  
✅ **Smart Suppression**: Turns off in Neutral/Reverse/pit lane  
✅ **WPF Configuration**: Live settings adjustment in SimHub  
✅ **Fallback Mode**: Works without hardware for testing  

## 📖 Documentation

| File | Purpose |
|------|---------|
| [csharp_plugin_design.md](csharp_plugin_design.md) | Architecture blueprint & class design |
| [requirements.md](requirements.md) | Original system requirements |
| [BusylightShiftLight/README.md](BusylightShiftLight/README.md) | Plugin build & installation guide |
| [python_original/README.md](python_original/README.md) | Reference Python implementation |

## 🛠️ Configuration

All settings are managed via SimHub's WPF settings panel:

- **Fallback Shift RPM**: 80-100% (default 95%)
- **Yellow Warning Threshold**: 70-95% of shift RPM (default 85%)
- **Flash Interval**: 20-200ms (default 50ms = 10Hz)
- **Color Customization**: Hex RGB for yellow and red

## 📦 Dependencies

- **.NET Framework 4.8** (SimHub requirement)
- **SimHub.Plugins.dll** (from SimHub installation)
- **SimHub.Plugins.WPF.dll** (optional, for UI)
- **BusylightSDK.dll** or **HidSharp** (for USB device communication)

## 🐍 Python Reference

The original Python implementation is archived in `python_original/` for reference. It served as the specification for this C# port but is **not** part of the plugin product.

See [python_original/README.md](python_original/README.md) for details.

## 🔧 Architecture Highlights

| Component | Responsibility |
|-----------|-----------------|
| **BusylightPlugin** | Telemetry processing, state machine, SimHub integration |
| **BusylightController** | USB device abstraction (SDK or HidSharp) |
| **PluginSettings** | Configuration persistence & WPF data binding |
| **SettingsView** | WPF user interface for settings adjustment |

## 📝 License & Attribution

**Original Python Author**: Antigravity  
**C# Port**: Converted from Python specification  

## 🐛 Troubleshooting

- **Plugin won't load**: Check SimHub DLL paths in .csproj
- **Device not detected**: Verify USB connection, try different port
- **Settings not saving**: Check write permissions in SimHub folder
- **Build errors**: Ensure .NET Framework 4.8 developer pack is installed

See [BusylightShiftLight/README.md](BusylightShiftLight/README.md#troubleshooting) for detailed troubleshooting.

---

**Status**: ✅ Complete C# implementation ready for development/deployment
