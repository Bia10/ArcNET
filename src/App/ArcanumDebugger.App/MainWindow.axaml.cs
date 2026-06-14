using System;
using System.Runtime.Versioning;
using ArcanumDebugger.App.ViewModels;
using Avalonia.Controls;

namespace ArcanumDebugger.App;

[SupportedOSPlatform("windows")]
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += OnClosed;
    }

    public MainWindow(MainWindowViewModel viewModel)
        : this() => DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

    private void OnClosed(object? sender, EventArgs e) => (DataContext as IDisposable)?.Dispose();
}
