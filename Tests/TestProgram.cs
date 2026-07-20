using System;

namespace BusylightShiftLight.Tests
{
    internal static class TestProgram
    {
        private static int _assertions;

        private static int Main()
        {
            try
            {
                EvaluateTelemetryStates();
                EvaluateFallbackThresholds();
                EvaluateShiftRpmSelection();
                EvaluateLightCommands();
                EvaluateFlashTiming();
                EvaluateInvalidColors();
                Console.WriteLine("PASS: " + _assertions + " assertions");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: " + ex.Message);
                return 1;
            }
        }

        private static void EvaluateTelemetryStates()
        {
            PluginSettings settings = new PluginSettings();
            Equal(ShiftLightState.Off, Evaluate(false, 0, "1", 9000, 10000, 8000, settings), "game stopped");
            Equal(ShiftLightState.Off, Evaluate(true, 1, "1", 9000, 10000, 8000, settings), "pit limiter");
            Equal(ShiftLightState.Off, Evaluate(true, 0, "N", 9000, 10000, 8000, settings), "neutral");
            Equal(ShiftLightState.Off, Evaluate(true, 0, " r ", 9000, 10000, 8000, settings), "reverse");
            Equal(ShiftLightState.Off, Evaluate(true, 0, "0", 9000, 10000, 8000, settings), "gear zero");
            Equal(ShiftLightState.Off, Evaluate(true, 0, "1", 6799, 10000, 8000, settings), "below yellow");
            Equal(ShiftLightState.Yellow, Evaluate(true, 0, "1", 6800, 10000, 8000, settings), "yellow boundary");
            Equal(ShiftLightState.FlashRed, Evaluate(true, 0, "1", 8000, 10000, 8000, settings), "red boundary");
        }

        private static void EvaluateFallbackThresholds()
        {
            PluginSettings settings = new PluginSettings();
            Equal(ShiftLightState.Off, Evaluate(true, 0, "2", 9000, 0, 0, settings), "no usable RPM limits");
            Equal(ShiftLightState.Off, Evaluate(true, 0, "2", 9000, 1, 0, settings), "normalized maximum RPM is rejected");
            Equal(ShiftLightState.Yellow, Evaluate(true, 0, "2", 8075, 10000, 0, settings), "fallback yellow boundary");
            Equal(ShiftLightState.FlashRed, Evaluate(true, 0, "2", 9500, 10000, 0, settings), "fallback red boundary");
        }

        private static void EvaluateShiftRpmSelection()
        {
            Equal(7600d, ShiftLightEvaluator.SelectShiftRpm(7600, 7800, 1, 8000), "current-gear redline takes priority");
            Equal(7800d, ShiftLightEvaluator.SelectShiftRpm(0, 7800, 1, 8000), "general redline fallback");
            Equal(7400d, ShiftLightEvaluator.SelectShiftRpm(0, 0, 7400, 8000), "plausible shift-light RPM fallback");
            Equal(0d, ShiftLightEvaluator.SelectShiftRpm(0, 0, 1, 8000), "normalized shift-light stage is rejected");
            Equal(0d, ShiftLightEvaluator.SelectShiftRpm(12000, 0, 0, 8000), "out-of-range redline is rejected");

            PluginSettings settings = new PluginSettings();
            double selected = ShiftLightEvaluator.SelectShiftRpm(0, 0, 1, 10000);
            Equal(ShiftLightState.Yellow, Evaluate(true, 0, "1", 8075, 10000, selected, settings), "first gear uses max-RPM fallback instead of stage value");
        }

        private static void EvaluateLightCommands()
        {
            PluginSettings settings = new PluginSettings();
            ShiftLightStateMachine machine = new ShiftLightStateMachine();
            DateTime now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            Equal(LightCommandType.Off, machine.Update(ShiftLightState.Off, settings, now).Type, "initial off");
            Equal(LightCommandType.None, machine.Update(ShiftLightState.Off, settings, now).Type, "stable off");

            LightCommand yellow = machine.Update(ShiftLightState.Yellow, settings, now);
            Equal(LightCommandType.Color, yellow.Type, "yellow command");
            Equal((byte)255, yellow.Red, "yellow red channel");
            Equal((byte)165, yellow.Green, "yellow green channel");
            Equal((byte)0, yellow.Blue, "yellow blue channel");
            Equal(LightCommandType.None, machine.Update(ShiftLightState.Yellow, settings, now).Type, "stable yellow");

            machine.Reset();
            Equal(LightCommandType.Color, machine.Update(ShiftLightState.Yellow, settings, now).Type, "reset replays output");
        }

        private static void EvaluateFlashTiming()
        {
            PluginSettings settings = new PluginSettings { FlashIntervalMs = 50 };
            ShiftLightStateMachine machine = new ShiftLightStateMachine();
            DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            LightCommand first = machine.Update(ShiftLightState.FlashRed, settings, start);
            Equal(LightCommandType.Color, first.Type, "flash starts on");
            Equal((byte)255, first.Red, "flash red channel");
            Equal(LightCommandType.None, machine.Update(ShiftLightState.FlashRed, settings, start.AddMilliseconds(49)).Type, "flash waits");
            Equal(LightCommandType.Off, machine.Update(ShiftLightState.FlashRed, settings, start.AddMilliseconds(50)).Type, "flash turns off");
            Equal(LightCommandType.Color, machine.Update(ShiftLightState.FlashRed, settings, start.AddMilliseconds(100)).Type, "flash turns on again");
        }

        private static void EvaluateInvalidColors()
        {
            PluginSettings settings = new PluginSettings { YellowHexColor = "invalid", RedHexColor = null };
            ShiftLightStateMachine machine = new ShiftLightStateMachine();
            DateTime now = DateTime.UtcNow;

            LightCommand yellow = machine.Update(ShiftLightState.Yellow, settings, now);
            Equal((byte)255, yellow.Red, "invalid yellow fallback red");
            Equal((byte)165, yellow.Green, "invalid yellow fallback green");
            Equal((byte)0, yellow.Blue, "invalid yellow fallback blue");

            LightCommand red = machine.Update(ShiftLightState.FlashRed, settings, now);
            Equal((byte)255, red.Red, "invalid red fallback red");
            Equal((byte)0, red.Green, "invalid red fallback green");
            Equal((byte)0, red.Blue, "invalid red fallback blue");
        }

        private static ShiftLightState Evaluate(bool running, int pitLimiter, string gear, double rpm, double maxRpm, double shiftRpm, PluginSettings settings)
        {
            return ShiftLightEvaluator.Evaluate(running, pitLimiter, gear, rpm, maxRpm, shiftRpm, settings);
        }

        private static void Equal<T>(T expected, T actual, string description)
        {
            _assertions++;
            if (!object.Equals(expected, actual))
            {
                throw new InvalidOperationException(description + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
