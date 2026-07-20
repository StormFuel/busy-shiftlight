using System;
using System.Globalization;

namespace BusylightShiftLight
{
    public enum ShiftLightState
    {
        Off,
        Yellow,
        FlashRed
    }

    public enum LightCommandType
    {
        None,
        Off,
        Color
    }

    public sealed class LightCommand
    {
        private LightCommand(LightCommandType type, byte red, byte green, byte blue)
        {
            Type = type;
            Red = red;
            Green = green;
            Blue = blue;
        }

        public LightCommandType Type { get; private set; }
        public byte Red { get; private set; }
        public byte Green { get; private set; }
        public byte Blue { get; private set; }

        public static LightCommand None()
        {
            return new LightCommand(LightCommandType.None, 0, 0, 0);
        }

        public static LightCommand Off()
        {
            return new LightCommand(LightCommandType.Off, 0, 0, 0);
        }

        public static LightCommand Color(byte red, byte green, byte blue)
        {
            return new LightCommand(LightCommandType.Color, red, green, blue);
        }
    }

    public static class ShiftLightEvaluator
    {
        public static double SelectShiftRpm(
            double currentGearRedLineRpm,
            double generalRedLineRpm,
            double shiftLightStageValue,
            double maxRpm)
        {
            if (IsPlausibleRpm(currentGearRedLineRpm, maxRpm))
            {
                return currentGearRedLineRpm;
            }

            if (IsPlausibleRpm(generalRedLineRpm, maxRpm))
            {
                return generalRedLineRpm;
            }

            return IsPlausibleRpm(shiftLightStageValue, maxRpm) ? shiftLightStageValue : 0;
        }

        public static ShiftLightState Evaluate(
            bool gameRunning,
            int pitLimiterOn,
            string gear,
            double rpm,
            double maxRpm,
            double configuredShiftRpm,
            PluginSettings settings)
        {
            if (!gameRunning || settings == null || pitLimiterOn > 0 || IsSuppressedGear(gear))
            {
                return ShiftLightState.Off;
            }

            double shiftRpm = configuredShiftRpm;
            if (shiftRpm <= 0)
            {
                if (maxRpm < 100)
                {
                    return ShiftLightState.Off;
                }

                shiftRpm = maxRpm * Clamp(settings.FallbackShiftRpmPercent, 0.01, 1.0);
            }

            double yellowThreshold = shiftRpm * Clamp(settings.YellowThresholdPercent, 0.01, 1.0);
            if (rpm >= shiftRpm)
            {
                return ShiftLightState.FlashRed;
            }

            return rpm >= yellowThreshold ? ShiftLightState.Yellow : ShiftLightState.Off;
        }

        private static bool IsSuppressedGear(string gear)
        {
            string normalized = (gear ?? string.Empty).Trim();
            return normalized.Equals("N", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("R", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("0", StringComparison.OrdinalIgnoreCase);
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static bool IsPlausibleRpm(double value, double maxRpm)
        {
            if (value < 100)
            {
                return false;
            }

            return maxRpm <= 0 || value <= maxRpm * 1.25;
        }
    }

    public sealed class ShiftLightStateMachine
    {
        private ShiftLightState? _lastState;
        private bool _flashOn;
        private DateTime _lastFlashTime = DateTime.MinValue;

        public LightCommand Update(ShiftLightState targetState, PluginSettings settings, DateTime utcNow)
        {
            if (settings == null)
            {
                Reset();
                return LightCommand.Off();
            }

            if (targetState == ShiftLightState.Off)
            {
                if (_lastState != ShiftLightState.Off)
                {
                    _lastState = ShiftLightState.Off;
                    _flashOn = false;
                    return LightCommand.Off();
                }

                return LightCommand.None();
            }

            if (targetState == ShiftLightState.Yellow)
            {
                if (_lastState != ShiftLightState.Yellow)
                {
                    _lastState = ShiftLightState.Yellow;
                    _flashOn = false;
                    return ParseColor(settings.YellowHexColor, 255, 165, 0);
                }

                return LightCommand.None();
            }

            int flashIntervalMs = Math.Max(20, settings.FlashIntervalMs);
            double elapsedMs = (utcNow - _lastFlashTime).TotalMilliseconds;
            if (_lastState != ShiftLightState.FlashRed || elapsedMs >= flashIntervalMs)
            {
                _flashOn = _lastState != ShiftLightState.FlashRed || !_flashOn;
                _lastState = ShiftLightState.FlashRed;
                _lastFlashTime = utcNow;
                return _flashOn
                    ? ParseColor(settings.RedHexColor, 255, 0, 0)
                    : LightCommand.Off();
            }

            return LightCommand.None();
        }

        public void Reset()
        {
            _lastState = null;
            _flashOn = false;
            _lastFlashTime = DateTime.MinValue;
        }

        private static LightCommand ParseColor(string value, byte fallbackRed, byte fallbackGreen, byte fallbackBlue)
        {
            string hex = (value ?? string.Empty).Trim().TrimStart('#');
            int color;
            if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out color))
            {
                return LightCommand.Color(fallbackRed, fallbackGreen, fallbackBlue);
            }

            return LightCommand.Color(
                (byte)((color >> 16) & 0xFF),
                (byte)((color >> 8) & 0xFF),
                (byte)(color & 0xFF));
        }
    }
}
