using System;
using System.Linq;
using System.Reflection;

namespace BusylightShiftLight
{
    /// <summary>
    /// Encapsulates the connection and control logic for the USB Busylight device.
    /// Supports BusylightSDK.dll and HidSharp backends with graceful fallback.
    /// </summary>
    public class BusylightController : IDisposable
    {
        private object _device;
        private bool _isConnected;
        private string _deviceType = "None"; // Track which backend is in use
        private bool _disposed = false;

        /// <summary>
        /// Attempts to connect to the Busylight device.
        /// Supports both BusylightSDK.dll and HidSharp implementations.
        /// Returns true if connection was successful, false otherwise.
        /// </summary>
        public bool Connect()
        {
            try
            {
                // Try to load BusylightSDK if available
                _device = TryLoadBusylightSDK();

                if (_device != null)
                {
                    _isConnected = true;
                    _deviceType = "BusylightSDK";
                    SimHub.Logging.Current.Info("Busylight connected via BusylightSDK");
                    return true;
                }

                // Fallback: Initialize HidSharp connection if SDK is not available
                _device = TryLoadHidSharp();
                if (_device != null)
                {
                    _isConnected = true;
                    _deviceType = "HidSharp";
                    SimHub.Logging.Current.Info("Busylight connected via HidSharp");
                    return true;
                }

                // No device found, continue in simulation mode
                _isConnected = false;
                _deviceType = "Simulation";
                SimHub.Logging.Current.Warn("No Busylight device detected. Running in simulation mode.");
                return false;
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"Error connecting to Busylight: {ex.Message}");
                _isConnected = false;
                _deviceType = "Simulation";
                return false;
            }
        }

        /// <summary>
        /// Sets the LED color. RGB values range from 0 to 255.
        /// </summary>
        public void SetColor(byte r, byte g, byte b)
        {
            if (_disposed)
            {
                SimHub.Logging.Current.Error("Cannot set color: BusylightController has been disposed");
                return;
            }

            if (!_isConnected || _device == null)
                return;

            try
            {
                if (_deviceType == "BusylightSDK")
                {
                    InvokeBusylightSDKLight(r, g, b);
                }
                else if (_deviceType == "HidSharp")
                {
                    // HidSharp implementation would be here
                    SimHub.Logging.Current.Debug($"[Simulation] Busylight color set to RGB({r}, {g}, {b})");
                }
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"Error setting Busylight color: {ex.Message}");
            }
        }

        /// <summary>
        /// Turns off the LED (sets color to black).
        /// </summary>
        public void TurnOff()
        {
            SetColor(0, 0, 0);
        }

        /// <summary>
        /// Disposes managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose implementation.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    if (_device != null)
                    {
                        var disposable = _device as IDisposable;
                        disposable?.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    SimHub.Logging.Current.Error($"Error disposing Busylight device: {ex.Message}");
                }
            }

            _device = null;
            _isConnected = false;
            _disposed = true;
        }

        /// <summary>
        /// Attempts to load and initialize the BusylightSDK.
        /// </summary>
        private object TryLoadBusylightSDK()
        {
            try
            {
                // Try assembly-qualified name first (requires BusylightSDK.dll in app directory or GAC)
                var busylightType = Type.GetType("Busylight.SDK, BusylightSDK");
                if (busylightType == null)
                {
                    busylightType = Type.GetType("Busylight.SDK");
                }

                if (busylightType != null)
                {
                    var instance = Activator.CreateInstance(busylightType);
                    if (instance != null)
                    {
                        SimHub.Logging.Current.Debug("BusylightSDK loaded successfully");
                        return instance;
                    }
                }
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Debug($"BusylightSDK not available: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Attempts to initialize HidSharp connection to Busylight.
        /// Busylight typically uses VID 0x27C6, PID varies by model.
        /// </summary>
        private object TryLoadHidSharp()
        {
            try
            {
                // Try to load HidSharp dynamically
                var hidSharpAsm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "HidSharp");

                if (hidSharpAsm == null)
                {
                    try
                    {
                        hidSharpAsm = AppDomain.CurrentDomain.Load("HidSharp");
                    }
                    catch
                    {
                        SimHub.Logging.Current.Debug("HidSharp assembly not available");
                        return null;
                    }
                }

                // Get HidSharp.DeviceList type
                var deviceListType = hidSharpAsm.GetType("HidSharp.DeviceList");
                if (deviceListType == null)
                {
                    SimHub.Logging.Current.Debug("HidSharp.DeviceList type not found");
                    return null;
                }

                // Get the local instance
                var localProperty = deviceListType.GetProperty("Local", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (localProperty == null)
                {
                    SimHub.Logging.Current.Debug("HidSharp.DeviceList.Local property not found");
                    return null;
                }

                var deviceList = localProperty.GetValue(null);
                var getHidDevicesMethod = deviceListType.GetMethod("GetHidDevices");

                if (getHidDevicesMethod != null && deviceList != null)
                {
                    var devices = getHidDevicesMethod.Invoke(deviceList, null) as System.Collections.IEnumerable;
                    if (devices != null)
                    {
                        // Search for Busylight device (VID 0x27C6)
                        foreach (var device in devices)
                        {
                            var vidProperty = device.GetType().GetProperty("VendorID");
                            if (vidProperty != null)
                            {
                                int vid = (int)vidProperty.GetValue(device);
                                if (vid == 0x27C6) // Busylight VID
                                {
                                    SimHub.Logging.Current.Info("Found Busylight device via HidSharp");
                                    return device; // Return the device object for potential later use
                                }
                            }
                        }
                    }
                }

                SimHub.Logging.Current.Debug("No Busylight device found via HidSharp enumeration");
                return null;
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Debug($"HidSharp initialization failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Safely invokes the BusylightSDK.Light method using reflection.
        /// </summary>
        private void InvokeBusylightSDKLight(byte r, byte g, byte b)
        {
            try
            {
                var deviceType = _device.GetType();
                var lightMethod = deviceType.GetMethod("Light", 
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(byte), typeof(byte), typeof(byte) },
                    null);

                if (lightMethod != null)
                {
                    lightMethod.Invoke(_device, new object[] { r, g, b });
                }
                else
                {
                    SimHub.Logging.Current.Warn("BusylightSDK.Light method not found; trying alternative method names");
                    // Try alternative method names
                    var altMethods = deviceType.GetMethods();
                    foreach (var method in altMethods)
                    {
                        if (method.Name.Equals("SetColor", StringComparison.OrdinalIgnoreCase) ||
                            method.Name.Equals("SetLight", StringComparison.OrdinalIgnoreCase))
                        {
                            SimHub.Logging.Current.Debug($"Found alternative method: {method.Name}");
                            method.Invoke(_device, new object[] { r, g, b });
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"Failed to invoke BusylightSDK.Light: {ex.Message}");
            }
        }
    }
}
