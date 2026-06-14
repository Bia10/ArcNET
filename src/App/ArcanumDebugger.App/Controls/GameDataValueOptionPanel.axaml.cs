using System.Windows.Input;
using ArcanumDebugger.App.ViewModels;
using Avalonia;
using Avalonia.Controls;

namespace ArcanumDebugger.App.Controls;

public partial class GameDataValueOptionPanel : UserControl
{
    public static readonly DirectProperty<GameDataValueOptionPanel, bool> ShowEntryProperty =
        AvaloniaProperty.RegisterDirect<GameDataValueOptionPanel, bool>(nameof(ShowEntry), panel => panel.ShowEntry);

    public static readonly DirectProperty<GameDataValueOptionPanel, bool> ShowEmptyStateProperty =
        AvaloniaProperty.RegisterDirect<GameDataValueOptionPanel, bool>(
            nameof(ShowEmptyState),
            panel => panel.ShowEmptyState
        );

    public static readonly StyledProperty<string> TitleTextProperty = AvaloniaProperty.Register<
        GameDataValueOptionPanel,
        string
    >(nameof(TitleText), string.Empty);

    public static readonly StyledProperty<string> EmptyStateTextProperty = AvaloniaProperty.Register<
        GameDataValueOptionPanel,
        string
    >(nameof(EmptyStateText), string.Empty);

    public static readonly StyledProperty<GameDataQuickPickEntry?> EntryProperty = AvaloniaProperty.Register<
        GameDataValueOptionPanel,
        GameDataQuickPickEntry?
    >(nameof(Entry));

    public static readonly StyledProperty<ICommand?> ApplyEntryCommandProperty = AvaloniaProperty.Register<
        GameDataValueOptionPanel,
        ICommand?
    >(nameof(ApplyEntryCommand));

    public static readonly StyledProperty<ICommand?> ApplyValueCommandProperty = AvaloniaProperty.Register<
        GameDataValueOptionPanel,
        ICommand?
    >(nameof(ApplyValueCommand));

    public string TitleText
    {
        get => GetValue(TitleTextProperty);
        set => SetValue(TitleTextProperty, value);
    }

    public string EmptyStateText
    {
        get => GetValue(EmptyStateTextProperty);
        set => SetValue(EmptyStateTextProperty, value);
    }

    public GameDataQuickPickEntry? Entry
    {
        get => GetValue(EntryProperty);
        set => SetValue(EntryProperty, value);
    }

    public ICommand? ApplyEntryCommand
    {
        get => GetValue(ApplyEntryCommandProperty);
        set => SetValue(ApplyEntryCommandProperty, value);
    }

    public ICommand? ApplyValueCommand
    {
        get => GetValue(ApplyValueCommandProperty);
        set => SetValue(ApplyValueCommandProperty, value);
    }

    public bool ShowEntry => Entry is not null;

    public bool ShowEmptyState => Entry is null;

    static GameDataValueOptionPanel()
    {
        EntryProperty.Changed.AddClassHandler<GameDataValueOptionPanel>(static (panel, _) => panel.RefreshEntryState());
    }

    public GameDataValueOptionPanel()
    {
        InitializeComponent();
    }

    private void RefreshEntryState()
    {
        RaisePropertyChanged(ShowEntryProperty, !ShowEntry, ShowEntry);
        RaisePropertyChanged(ShowEmptyStateProperty, !ShowEmptyState, ShowEmptyState);
    }
}
