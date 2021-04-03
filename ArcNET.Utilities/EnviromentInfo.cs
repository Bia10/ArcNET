using System;

namespace ArcNET.Utilities
{
    public class EnvironmentInfo
    {
        public readonly DisplaySettings DisplaySettings;
        public readonly OperatingSystem OperatingSystem;

        public EnvironmentInfo()
        {
            DisplaySettings = new DisplaySettings();
            OperatingSystem = Environment.OSVersion;
        }

        public void Print()
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