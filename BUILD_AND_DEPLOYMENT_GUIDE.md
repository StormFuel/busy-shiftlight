# Build & Deployment Guide

Step-by-step instructions for building the C# SimHub plugin and deploying it to SimHub.

## Prerequisites

- Visual Studio 2019 or later (Community Edition is fine)
- .NET Framework 4.8 Developer Pack
- SimHub installed at `C:\Program Files (x86)\SimHub`
- Administrator access for deployment

## Step 1: Prepare Your Build Environment

### 1.1 Verify .NET Framework 4.8 is installed

```powershell
# Check if .NET Framework 4.8 is present
Get-ChildItem "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4" -ErrorAction SilentlyContinue | Select-Object PSChildName
```

If not installed, download from: https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48

### 1.2 Verify SimHub installation

```powershell
Test-Path "C:\Program Files (x86)\SimHub\SimHub.Plugins.dll"
```

Should return `True`. If not, update the path in Step 2.

## Step 2: Configure Project References

### 2.1 Open the project file

The SimHub references are already enabled in `BusylightShiftLight.csproj`. If SimHub is installed in a different location, update their `HintPath` values. The official Plenom SDK is restored through the pinned `com.plenom.BusylightSDK` package reference.

### 2.2 Verify paths

If SimHub is installed in a different location, update the `HintPath` values.

## Step 3: Build the Project

### 3.1 Open in Visual Studio

1. Launch Visual Studio
2. Open `BusylightShiftLight/BusylightShiftLight.csproj`
3. Wait for the project to load and restore any NuGet packages

### 3.2 Verify references load

- Right-click project → **Properties**
- Go to **References** tab
- Verify `SimHub.Plugins`, `GameReaderCommon`, `SimHub.Logging`, and `BusylightSDK` appear without warnings

### 3.3 Build in Release mode

```
Build → Rebuild Solution
```

Or use keyboard shortcut: `Ctrl+Shift+B`

### 3.4 Check for build errors

The output window should show:

```
========== Rebuild All: 1 succeeded, 0 failed ==========
```

Build output will be at:
```
BusylightShiftLight\bin\Release\BusylightShiftLight.dll
```

## Step 4: Run Tests

Build and run `Tests\BusylightShiftLight.Tests.csproj` for the telemetry and flash-state assertions. With hardware attached, build and run `Tests\BusylightShiftLight.HardwareSmoke.csproj`; it sends yellow, red, and off commands through the official SDK.

## Step 5: Deploy to SimHub

### 5.1 Copy the DLL to SimHub

```powershell
# Run as Administrator
$source = "BusylightShiftLight\bin\Release\BusylightShiftLight.dll"
$dest = "C:\Program Files (x86)\SimHub\"

Copy-Item -Path $source -Destination $dest -Force
```

### 5.2 Copy the required SDK dependency

`BusylightSDK.dll` is required alongside the plugin DLL:

```powershell
$deps = @("BusylightSDK.dll")
foreach ($dep in $deps) {
    $src = "BusylightShiftLight\bin\Release\$dep"
    if (Test-Path $src) {
        Copy-Item -Path $src -Destination "C:\Program Files (x86)\SimHub\" -Force
    }
}
```

## Step 6: Activate in SimHub

### 6.1 Start or restart SimHub

```powershell
# Stop any running SimHub instance
Stop-Process -Name "SimHub" -Force -ErrorAction SilentlyContinue

# Wait a moment
Start-Sleep -Seconds 2

# Start SimHub
& "C:\Program Files (x86)\SimHub\SimHub.exe"
```

### 6.2 Enable the plugin

1. In SimHub, go to **Settings → Plugins**
2. Find **"Busylight Shift Indicator"** in the list
3. Toggle the switch to **Enabled**
4. SimHub will ask to restart (if needed)
5. After restart, a settings panel will appear in the left sidebar

### 6.3 Verify installation

Check SimHub's log file:

```powershell
$logPath = "C:\Program Files (x86)\SimHub\Logs\SimHub.log"
Get-Content $logPath -Tail 50 | Select-String "Busylight"
```

You should see:
```
[INFO] Busylight connected successfully.
```

Or if no device:
```
[WARN] Busylight device not detected.
```

Both indicate successful plugin initialization.

## Step 7: Configure Settings (Optional)

1. In SimHub's left sidebar, find **"Busylight Shift Indicator"**
2. Adjust sliders for:
   - Fallback Shift RPM %
   - Yellow Warning Threshold %
   - Flash Interval (ms)
3. Enter custom hex colors for Yellow and Red
4. Settings auto-save

## Troubleshooting Build Issues

### "SimHub.Plugins.dll not found"

- Verify SimHub is installed at `C:\Program Files (x86)\SimHub`
- Check that `HintPath` in .csproj matches your installation
- Try updating the path:
  ```xml
  <HintPath>YOUR_SIMHUB_PATH\SimHub.Plugins.dll</HintPath>
  ```

### Build fails with "missing reference"

- Right-click project → **Unload Project**
- Right-click again → **Edit Project File**
- Verify all `<Reference>` elements have valid `<HintPath>` values
- Save and reload the project

### "TargetFrameworkVersion mismatch"

- Ensure Visual Studio has .NET Framework 4.8 Developer Pack installed
- Go to **Tools → Get Tools and Features** → **Individual Components**
- Search for ".NET Framework 4.8" and install if missing

## Troubleshooting Plugin Loading

### Plugin doesn't appear in SimHub's plugins list

1. Check file location: `C:\Program Files (x86)\SimHub\BusylightShiftLight.dll`
2. Verify no build errors (see Step 3.4)
3. Check SimHub log: `%APPDATA%\SimHub\Logs\SimHub.log`
4. Restart SimHub service:
   ```powershell
   Restart-Service SimHub -Force -ErrorAction SilentlyContinue
   ```

### Settings panel not showing

- Ensure `SettingsView.xaml` is properly compiled
- Check for XAML validation errors in Visual Studio
- Verify WPF references are loaded

### Device not connecting

- Ensure Busylight is connected via USB
- Try different USB ports
- Check Windows Device Manager for USB Busylight device
- Verify driver is installed (if using custom SDK)

## Rollback / Uninstall

```powershell
# Remove the plugin DLL
Remove-Item "C:\Program Files (x86)\SimHub\BusylightShiftLight.dll" -Force

# Optionally remove dependencies
Remove-Item "C:\Program Files (x86)\SimHub\BusylightSDK.dll" -Force -ErrorAction SilentlyContinue

# Restart SimHub
Restart-Service SimHub -Force
```

## Next Steps

- Test the plugin with a racing sim connected to SimHub
- Adjust configuration thresholds to your preference
- Monitor SimHub logs for any warnings/errors

For more details, see [BusylightShiftLight/README.md](BusylightShiftLight/README.md)
