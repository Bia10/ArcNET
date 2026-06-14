using System;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace ArcanumDebugger.App;

[SupportedOSPlatform("windows")]
public partial class App : Application
{
    private readonly Func<Window> mainWindowFactory;

    public App()
        : this(static () => new MainWindow()) { }

    public App(Func<Window> mainWindowFactory) =>
        this.mainWindowFactory = mainWindowFactory ?? throw new ArgumentNullException(nameof(mainWindowFactory));

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = mainWindowFactory();

        base.OnFrameworkInitializationCompleted();
    }
}
