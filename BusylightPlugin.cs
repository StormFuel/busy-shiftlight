using SimHub.Plugins;
using GameReaderCommon;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BusylightShiftLight
{
    /// <summary>
    /// Main SimHub plugin class for Busylight Shift Light integration.
    /// Implements IDataPlugin for telemetry hooks and IWPFSettingsV2 for UI integration.
    /// </summary>
    [PluginDescription("Controls a physical Busylight device as an adaptive racing shift light.")]
    [PluginAuthor("StormFuel")]
    [PluginName("Busylight Shift Indicator")]
    public class BusylightPlugin : IDataPlugin, IWPFSettingsV2
    {
        private BusylightController _lightController;
        private PluginSettings _settings;

        private readonly ShiftLightStateMachine _stateMachine = new ShiftLightStateMachine();

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
                    ApplyLightState(ShiftLightState.Off);
                    return;
                }

                double shiftRpm = ShiftLightEvaluator.SelectShiftRpm(
                    data.NewData.CarSettings_CurrentGearRedLineRPM,
                    data.NewData.CarSettings_RedLineRPM,
                    data.NewData.CarSettings_RPMShiftLight1,
                    data.NewData.CarSettings_MaxRPM);

                ShiftLightState targetState = ShiftLightEvaluator.Evaluate(
                    data.GameRunning,
                    data.NewData.PitLimiterOn,
                    data.NewData.Gear,
                    data.NewData.Rpms,
                    data.NewData.CarSettings_MaxRPM,
                    shiftRpm,
                    _settings);

                ApplyLightState(targetState);
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"Error in DataUpdate: {ex.Message}");
                ApplyLightState(ShiftLightState.Off);
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
        private void ApplyLightState(ShiftLightState targetState)
        {
            LightCommand command = _stateMachine.Update(targetState, _settings, DateTime.UtcNow);
            bool applied = true;

            if (command.Type == LightCommandType.Off)
            {
                applied = _lightController != null && _lightController.TurnOff();
            }
            else if (command.Type == LightCommandType.Color)
            {
                applied = _lightController != null && _lightController.SetColor(command.Red, command.Green, command.Blue);
            }

            if (!applied)
            {
                _stateMachine.Reset();
            }
        }

        private static readonly ImageSource PluginIcon = LoadPluginIcon();

        private static ImageSource LoadPluginIcon()
        {
            using (Stream stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("BusylightShiftLight.Assets.BusylightPluginIcon.png"))
            {
                if (stream == null)
                {
                    return null;
                }

                var icon = new BitmapImage();
                icon.BeginInit();
                icon.CacheOption = BitmapCacheOption.OnLoad;
                icon.StreamSource = stream;
                icon.EndInit();
                icon.Freeze();
                return icon;
            }
        }

        public ImageSource PictureIcon => PluginIcon;
        public string LeftMenuTitle => "Busylight";
    }
}
