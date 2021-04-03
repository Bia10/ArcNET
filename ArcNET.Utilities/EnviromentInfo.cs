using System;

namespace ArcNET.Utilities
{
    public static class EnvironmentInfo
    {
        private static readonly DisplaySettings DisplaySettings;
        private static readonly OperatingSystem OperatingSystem;

        static EnvironmentInfo()
        {
            DisplaySettings = new DisplaySettings();
            OperatingSystem = Environment.OSVersion;
        }

        public static void Print()
        {
            var osMajorVersion = OperatingSystem.Version.Major;
            var osMinorVersion = OperatingSystem.Version.Minor;
            var osBuildVersion = OperatingSystem.Version.Build;

            AnsiConsoleExtensions.Log($"Major: {osMajorVersion} " +
                                      $"Minor: {osMinorVersion} " +
                                      $"OsBuildVersion: {osBuildVersion}", "info");

            DisplaySettings.PrintCurrentDisplaySettings();
        }
    }
}