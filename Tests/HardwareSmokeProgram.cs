using System;
using System.Threading;

namespace BusylightShiftLight.HardwareSmoke
{
    internal static class HardwareSmokeProgram
    {
        [STAThread]
        private static int Main()
        {
            try
            {
                BusylightPlugin plugin = new BusylightPlugin();
                if (plugin.PictureIcon == null || plugin.PictureIcon.Width <= 0 || plugin.PictureIcon.Height <= 0)
                {
                    Console.Error.WriteLine("Plugin icon did not load from the embedded WPF resource.");
                    return 1;
                }

                Console.WriteLine("Plugin icon loaded: " + plugin.PictureIcon.Width + "x" + plugin.PictureIcon.Height);

                using (BusylightController controller = new BusylightController())
                {
                    bool connected = controller.Connect();
                    Console.WriteLine("SDK initialized. Hardware connected: " + connected);

                    if (connected)
                    {
                        if (!controller.SetColor(255, 165, 0))
                        {
                            Console.Error.WriteLine("SDK found hardware but the yellow command failed.");
                            return 1;
                        }

                        Thread.Sleep(250);

                        if (!controller.SetColor(255, 0, 0))
                        {
                            Console.Error.WriteLine("SDK found hardware but the red command failed.");
                            return 1;
                        }

                        Thread.Sleep(250);

                        if (!controller.TurnOff())
                        {
                            Console.Error.WriteLine("SDK found hardware but the off command failed.");
                            return 1;
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("SDK smoke test failed: " + ex);
                return 1;
            }
        }
    }
}
