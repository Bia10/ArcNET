using System;
using System.Runtime.InteropServices;

namespace ArcNET.Utilities
{
    public static class EnvironmentInfo
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref Devmode devMode);

        [StructLayout(LayoutKind.Sequential)]
        private struct Devmode
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public readonly string dmDeviceName;
            public readonly short dmSpecVersion;
            public readonly short dmDriverVersion;
            public short dmSize;
            public readonly short dmDriverExtra;
            public readonly int dmFields;
            public readonly int dmPositionX;
            public readonly int dmPositionY;
            public readonly int dmDisplayOrientation;
            public readonly int dmDisplayFixedOutput;
            public readonly short dmColor;
            public readonly short dmDuplex;
            public readonly short dmYResolution;
            public readonly short dmTTOption;
            public readonly short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public readonly string dmFormName;
            public readonly short dmLogPixels;
            public readonly int dmBitsPerPel;
            public readonly int dmPelsWidth;
            public readonly int dmPelsHeight;
            public readonly int dmDisplayFlags;
            public readonly int dmDisplayFrequency;
            public readonly int dmICMMethod;
            public readonly int dmICMIntent;
            public readonly int dmMediaType;
            public readonly int dmDitherType;
            public readonly int dmReserved1;
            public readonly int dmReserved2;
            public readonly int dmPanningWidth;
            public readonly int dmPanningHeight;
        }

        public static void Print()
        {
            var os = Environment.OSVersion;
            var osMajorVersion = os.Version.Major;
            var osMinorVersion = os.Version.Minor;
            var osBuildVersion = os.Version.Build;

            AnsiConsoleExtensions.Log($"Major: {osMajorVersion}" +
                                      $"Minor: {osMinorVersion}" +
                                      $"OsBuildVersion: {osBuildVersion}", "info");

            Devmode devMode = default;
            devMode.dmSize = (short)Marshal.SizeOf(devMode);
            EnumDisplaySettings(null, -1, ref devMode); // -1 = ENUM_CURRENT_SETTINGS

            var deviceName = devMode.dmDeviceName;
            var screenWidth = devMode.dmPelsWidth.ToString();
            var screenHeight = devMode.dmPelsHeight.ToString();

            AnsiConsoleExtensions.Log($"ScreenWidth: {screenWidth}" +
                                      $"ScreenHeight: {screenHeight}", "info");
        }
    }
}