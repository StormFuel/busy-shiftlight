using SimHub.Plugins;
using GameReaderCommon;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace BusylightShiftLight
{
    /// <summary>
    /// Main SimHub plugin class for Busylight Shift Light integration.
    /// Implements IDataPlugin for telemetry hooks and IWPFSettingsV2 for UI integration.
    /// </summary>
    [PluginDescription("Controls a physical Busylight device as an adaptive racing shift light.")]
    [PluginAuthor("Antigravity")]
    [PluginName("Busylight Shift Indicator")]
    public class BusylightPlugin : IDataPlugin, IWPFSettingsV2
    {
        private BusylightController _lightController;
        private PluginSettings _settings;

        // State Tracking variables
        private string _lastState = "OFF";
        private bool _flashOn = false;
        private DateTime _lastFlashTime = DateTime.MinValue;

        /// <summary>
        /// Plugin manager instance, set by SimHub via IPlugin.
        /// </summary>
        public PluginManager PluginManager { get; set; }

        /// <summary>
        /// Called when SimHub starts up.
        /// </summary>
        public void Init(PluginManager pluginManager)
        {
            try
            {
                // Store plugin manager for later use
                this.PluginManager = pluginManager;

                // Load settings from SimHub's common settings store
                // ReadCommonSettings is an extension method on IPlugin (via IPluginExtensions)
                _settings = this.ReadCommonSettings<PluginSettings>("BusylightShiftLightSettings", () => new PluginSettings());

                // Initialize Busylight controller
                _lightController = new BusylightController();
                if (_lightController.Connect())
                {
                    SimHub.Logging.Current.Info("Busylight connected successfully.");
                }
                else
                {
                    SimHub.Logging.Current.Warn("Busylight device not detected. Plugin will continue running in simulation mode.");
                }
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"Error initializing Busylight plugin: {ex.Message}");
            }
        }

        /// <summary>
        /// Called every game frame with the latest telemetry data.
        /// This is where we determine the light state based on RPM thresholds.
        /// </summary>
        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            try
            {
                // If game is not running or no data available, turn off light
                if (data == null || !data.GameRunning || data.NewData == null)
                {
                    SetLightState("OFF");
                    return;
                }

                // 1. Check for suppression conditions: Pit Limiter, Neutral, Reverse, Gear 0
                // Real SimHub API: PitLimiterOn is int, Gear is string
                bool isPitLimiterOn = data.NewData.PitLimiterOn > 0;

                string gear = data.NewData.Gear ?? "";
                bool isNeutral = gear.Equals("N", StringComparison.OrdinalIgnoreCase);
                bool isReverse = gear.Equals("R", StringComparison.OrdinalIgnoreCase);
                bool isGear0 = gear.Equals("0", StringComparison.OrdinalIgnoreCase);

                if (isPitLimiterOn || isNeutral || isReverse || isGear0)
                {
                    SetLightState("OFF");
                    return;
                }

                // 2. Fetch and validate RPM thresholds
                double rpms = data.NewData.Rpms;
                double maxRpm = data.NewData.CarSettings_MaxRPM;
                // Real API uses CarSettings_RPMShiftLight1 (not CarSettings_ShiftLightRPM)
                double shiftRpm = data.NewData.CarSettings_RPMShiftLight1;

                // Apply fallback shift RPM if not provided
                if (shiftRpm <= 0)
                {
                    if (maxRpm > 0)
                    {
                        shiftRpm = maxRpm * _settings.FallbackShiftRpmPercent;
                    }
                    else
                    {
                        SetLightState("OFF");
                        return;
                    }
                }

                double yellowThreshold = shiftRpm * _settings.YellowThresholdPercent;

                // 3. Determine and apply state based on RPM thresholds
                if (rpms >= shiftRpm)
                {
                    SetLightState("FLASH_RED");
                }
                else if (rpms >= yellowThreshold)
                {
                    SetLightState("YELLOW");
                }
                else
                {
                    SetLightState("OFF");
                }
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"Error in DataUpdate: {ex.Message}");
                SetLightState("OFF");
            }
        }

        /// <summary>
        /// Called when SimHub closes. Cleanup and save settings.
        /// </summary>
        public void End(PluginManager pluginManager)
        {
            try
            {
                _lightController?.TurnOff();
                _lightController?.Dispose();
                // SaveCommonSettings is an extension method on IPlugin (via IPluginExtensions)
                this.SaveCommonSettings("BusylightShiftLightSettings", _settings);
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"Error in End: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns the WPF user control for the settings panel in SimHub's left sidebar.
        /// </summary>
        public Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsView(this, _settings);
        }

        /// <summary>
        /// Manages state transitions and applies light effects.
        /// </summary>
        private void SetLightState(string targetState)
        {
            if (targetState == "OFF")
            {
                if (_lastState != "OFF")
                {
                    _lightController.TurnOff();
                    _lastState = "OFF";
                }
            }
            else if (targetState == "YELLOW")
            {
                if (_lastState != "YELLOW")
                {
                    var color = ConvertHexToRGB(_settings.YellowHexColor);
                    _lightController.SetColor(color.R, color.G, color.B);
                    _lastState = "YELLOW";
                }
            }
            else if (targetState == "FLASH_RED")
            {
                var now = DateTime.UtcNow;
                var elapsedMs = (now - _lastFlashTime).TotalMilliseconds;

                if (_lastState != "FLASH_RED" || elapsedMs >= _settings.FlashIntervalMs)
                {
                    _flashOn = (_lastState == "FLASH_RED") ? !_flashOn : true;
                    _lastFlashTime = now;
                    _lastState = "FLASH_RED";

                    if (_flashOn)
                    {
                        var color = ConvertHexToRGB(_settings.RedHexColor);
                        _lightController.SetColor(color.R, color.G, color.B);
                    }
                    else
                    {
                        _lightController.TurnOff();
                    }
                }
            }
        }

        /// <summary>
        /// Converts a hex color string (#RRGGBB) to RGB byte tuple.
        /// </summary>
        private (byte R, byte G, byte B) ConvertHexToRGB(string hex)
        {
            try
            {
                hex = hex.Replace("#", "");
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                return (r, g, b);
            }
            catch
            {
                return (255, 255, 255); // Fallback to white if hex is invalid
            }
        }

        // Implement IWPFSettingsV2
        // Real API uses PictureIcon, not PluginIcon
        public ImageSource PictureIcon => null;
        public string LeftMenuTitle => "Busylight";
    }
}
