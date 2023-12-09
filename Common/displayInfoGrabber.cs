using System.Runtime.InteropServices;

namespace GTAIVSetupUtilityWPF.Common
{

    // this code has been brought to you by chatgpt:tm: because i have NO FUCKING IDEA WHAT ANY OF THIS MEANS
    // if it breaks i'm switching my gods
    public static class DisplayInfo
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DisplayDevice
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayDevices(string? lpDevice, int iDevNum, ref DisplayDevice lpDisplayDevice, int dwFlags);

        public static void GetPrimaryDisplayInfo(out int width, out int height, out int refreshRate)
        {
            DisplayDevice primaryDevice = new DisplayDevice();

            if (EnumDisplayDevices(null, 0, ref primaryDevice, 0))
            {
                Devmode devMode = new Devmode();

                if (EnumDisplaySettings(primaryDevice.DeviceName, -1, ref devMode))
                {
                    width = devMode.dmPelsWidth;
                    height = devMode.dmPelsHeight;
                    refreshRate = devMode.dmDisplayFrequency;
                    return;
                }
            }

            // If we couldn't retrieve the information, set default values or throw an exception.
            width = 1920; // Default width
            height = 1080; // Default height
            refreshRate = 60; // Default refresh rate
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Devmode
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFrequency;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref Devmode lpDevMode);
    }
}