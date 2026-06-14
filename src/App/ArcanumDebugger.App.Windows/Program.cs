using System;
using System.Runtime.Versioning;
using ArcanumDebugger.App;
using ArcanumDebugger.App.ViewModels;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Windows;
using Avalonia;

namespace ArcanumDebugger.App.Windows;

[SupportedOSPlatform("windows")]
internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp(Func<IDiagnosticsServices>? createDiagnosticsServices = null)
    {
        createDiagnosticsServices ??= static () => new WindowsDiagnosticsServices();

        return AppBuilder
            .Configure(() =>
            {
                var diagnosticsServices = createDiagnosticsServices();
                return new App(() => new MainWindow(new MainWindowViewModel(diagnosticsServices)));
            })
            .UsePlatformDetect()
            .LogToTrace();
    }
}
