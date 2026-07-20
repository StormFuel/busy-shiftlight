using System;

namespace BusylightShiftLight
{
    /// <summary>
    /// Configuration settings for the Busylight Shift Light plugin.
    /// SimHub automatically serializes/deserializes this to/from JSON.
    /// </summary>
    public class PluginSettings
    {
        /// <summary>
        /// Percentage of maximum RPM used as shift point if car settings don't provide one.
        /// Default: 0.95 (95% of max RPM)
        /// </summary>
        public double FallbackShiftRpmPercent { get; set; } = 0.95;

        /// <summary>
        /// Percentage of shift RPM at which the yellow warning light activates.
        /// Default: 0.85 (85% of shift RPM)
        /// </summary>
        public double YellowThresholdPercent { get; set; } = 0.85;

        /// <summary>
        /// Flash interval in milliseconds for the red warning light.
        /// Default: 50 (10Hz flash frequency, 50ms on/off toggle)
        /// </summary>
        public int FlashIntervalMs { get; set; } = 50;

        /// <summary>
        /// Hex color code for the yellow warning light. Format: #RRGGBB
        /// </summary>
        public string YellowHexColor { get; set; } = "#FFA500"; // Orange/Yellow

        /// <summary>
        /// Hex color code for the red warning light. Format: #RRGGBB
        /// </summary>
        public string RedHexColor { get; set; } = "#FF0000"; // Red
    }
}
