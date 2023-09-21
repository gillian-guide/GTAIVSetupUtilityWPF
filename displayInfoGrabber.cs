using System.Runtime.InteropServices;

// this code has been brought to you by chatgpt:tm: because i have NO FUCKING IDEA WHAT ANY OF THIS MEANS
// if it breaks i'm switching my gods
public class DisplayInfo
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayDevices(string lpDevice, int iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, int dwFlags);

    public static void GetPrimaryDisplayInfo(out int width, out int height, out int refreshRate)
    {
        DISPLAY_DEVICE primaryDevice = new DISPLAY_DEVICE();
        primaryDevice.cb = Marshal.SizeOf(primaryDevice);

        if (EnumDisplayDevices(null, 0, ref primaryDevice, 0))
        {
            DEVMODE devMode = new DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf(devMode);

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
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);
}
