using System.Windows.Input;
using ArcanumDebugger.App.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace ArcanumDebugger.App.Controls;

public partial class GameDataSupportedInputPanel : UserControl
{
    public static readonly StyledProperty<GameDataSupportedInputPanelState> StateProperty = AvaloniaProperty.Register<
        GameDataSupportedInputPanel,
        GameDataSupportedInputPanelState
    >(nameof(State), new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, []));

    public static readonly StyledProperty<string> FilterTextProperty = AvaloniaProperty.Register<
        GameDataSupportedInputPanel,
        string
    >(nameof(FilterText), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> ApplyEntryCommandProperty = AvaloniaProperty.Register<
        GameDataSupportedInputPanel,
        ICommand?
    >(nameof(ApplyEntryCommand));

    public static readonly StyledProperty<ICommand?> BrowseCommandProperty = AvaloniaProperty.Register<
        GameDataSupportedInputPanel,
        ICommand?
    >(nameof(BrowseCommand));

    public static readonly StyledProperty<ICommand?> ApplyValueCommandProperty = AvaloniaProperty.Register<
        GameDataSupportedInputPanel,
        ICommand?
    >(nameof(ApplyValueCommand));

    public GameDataSupportedInputPanelState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public string FilterText
    {
        get => GetValue(FilterTextProperty);
        set => SetValue(FilterTextProperty, value);
    }

    public ICommand? ApplyEntryCommand
    {
        get => GetValue(ApplyEntryCommandProperty);
        set => SetValue(ApplyEntryCommandProperty, value);
    }

    public ICommand? BrowseCommand
    {
        get => GetValue(BrowseCommandProperty);
        set => SetValue(BrowseCommandProperty, value);
    }

    public ICommand? ApplyValueCommand
    {
        get => GetValue(ApplyValueCommandProperty);
        set => SetValue(ApplyValueCommandProperty, value);
    }

    public GameDataSupportedInputPanel()
    {
        InitializeComponent();
    }
}
