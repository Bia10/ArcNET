using System;
using System.Runtime.Versioning;
using Avalonia;

namespace ArcanumDebugger.App;

[SupportedOSPlatform("windows")]
internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
