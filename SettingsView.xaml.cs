using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BusylightShiftLight
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// WPF user control that provides a configuration panel inside SimHub's dashboard.
    /// </summary>
    public partial class SettingsView : UserControl
    {
        private BusylightPlugin _plugin;
        private PluginSettings _settings;

        public SettingsView(BusylightPlugin plugin, PluginSettings settings)
        {
            InitializeComponent();
            _plugin = plugin;
            _settings = settings;
            InitializeBindings();
        }

        private void InitializeBindings()
        {
            // Fallback Shift RPM Slider
            FallbackShiftRpmSlider.Value = _settings.FallbackShiftRpmPercent;
            FallbackShiftRpmSlider.ValueChanged += (s, e) =>
            {
                _settings.FallbackShiftRpmPercent = FallbackShiftRpmSlider.Value;
                FallbackShiftRpmValue.Text = _settings.FallbackShiftRpmPercent.ToString("F2");
            };

            // Yellow Threshold Slider
            YellowThresholdSlider.Value = _settings.YellowThresholdPercent;
            YellowThresholdSlider.ValueChanged += (s, e) =>
            {
                _settings.YellowThresholdPercent = YellowThresholdSlider.Value;
                YellowThresholdValue.Text = _settings.YellowThresholdPercent.ToString("F2");
            };

            // Flash Interval Slider
            FlashIntervalSlider.Value = _settings.FlashIntervalMs;
            FlashIntervalSlider.ValueChanged += (s, e) =>
            {
                _settings.FlashIntervalMs = (int)FlashIntervalSlider.Value;
                FlashIntervalValue.Text = $"{_settings.FlashIntervalMs} ms";
            };

            // Yellow Color Hex
            YellowColorHex.Text = _settings.YellowHexColor;
            YellowColorHex.TextChanged += (s, e) =>
            {
                _settings.YellowHexColor = YellowColorHex.Text;
                UpdateColorPreview(YellowColorHex.Text, YellowColorPreview);
            };

            // Red Color Hex
            RedColorHex.Text = _settings.RedHexColor;
            RedColorHex.TextChanged += (s, e) =>
            {
                _settings.RedHexColor = RedColorHex.Text;
                UpdateColorPreview(RedColorHex.Text, RedColorPreview);
            };

            // Set initial display values
            FallbackShiftRpmValue.Text = _settings.FallbackShiftRpmPercent.ToString("F2");
            YellowThresholdValue.Text = _settings.YellowThresholdPercent.ToString("F2");
            FlashIntervalValue.Text = $"{_settings.FlashIntervalMs} ms";
        }

        private void UpdateColorPreview(string hexColor, Rectangle preview)
        {
            try
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
                preview.Fill = brush;
            }
            catch
            {
                // Invalid color format, keep existing preview
            }
        }
    }
}
