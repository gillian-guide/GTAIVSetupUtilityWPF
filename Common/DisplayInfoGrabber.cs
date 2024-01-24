using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace GTAIVSetupUtilityWPF.Common
{

    // this code has been brought to you by chatgpt:tm: because i have NO FUCKING IDEA WHAT ANY OF THIS MEANS
    // if it breaks i'm switching my gods
    public static class DisplayInfo
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DISPLAY_DEVICE
        {
            public int cb;
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
        private static extern bool EnumDisplayDevices(string? lpDevice, int iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, int dwFlags);

        public static void GetPrimaryDisplayInfo(out int width, out int height, out int refreshRate)
        {
            DISPLAY_DEVICE primaryDevice = new DISPLAY_DEVICE();
            primaryDevice.cb = Marshal.SizeOf(primaryDevice);

            // default values if this doesn't work
            width = 1920;
            height = 1080;
            refreshRate = 60;

            if (EnumDisplayDevices(null, 0, ref primaryDevice, 0))
            {
                DEVMODE devMode = new DEVMODE();
                devMode.dmSize = (short)Marshal.SizeOf(devMode);

                if (EnumDisplaySettings(primaryDevice.DeviceName, -1, ref devMode))
                {
                    width = devMode.dmPelsWidth;
                    height = devMode.dmPelsHeight;
                    refreshRate = devMode.dmDisplayFrequency;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFrequency;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);
    }
}