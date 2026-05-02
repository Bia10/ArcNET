namespace ArcNET.Editor;

/// <summary>
/// Persisted object-placement workflow state for one project map view.
/// </summary>
public sealed class EditorProjectMapObjectPlacementToolState
{
    /// <summary>
    /// Selected placement workflow mode.
    /// </summary>
    public EditorProjectMapObjectPlacementMode Mode { get; init; } =
        EditorProjectMapObjectPlacementMode.SinglePlacement;

    /// <summary>
    /// Selected single-placement request when <see cref="Mode"/> is <see cref="EditorProjectMapObjectPlacementMode.SinglePlacement"/>.
    /// </summary>
    public EditorObjectPalettePlacementRequest? PlacementRequest { get; init; }

    /// <summary>
    /// Selected reusable placement set when <see cref="Mode"/> is <see cref="EditorProjectMapObjectPlacementMode.PlacementSet"/>.
    /// </summary>
    public EditorObjectPalettePlacementSet? PlacementSet { get; init; }

    /// <summary>
    /// Persisted preset library available to the map view.
    /// </summary>
    public IReadOnlyList<EditorObjectPalettePlacementPreset> PresetLibrary { get; init; } = [];

    /// <summary>
    /// Selected preset identifier when <see cref="Mode"/> is <see cref="EditorProjectMapObjectPlacementMode.PlacementPreset"/>.
    /// </summary>
    public string? SelectedPresetId { get; init; }

    /// <summary>
    /// Resolves the selected preset from <see cref="PresetLibrary"/>, or <see langword="null"/> when unavailable.
    /// </summary>
    public EditorObjectPalettePlacementPreset? FindSelectedPreset() =>
        string.IsNullOrWhiteSpace(SelectedPresetId)
            ? null
            : PresetLibrary.FirstOrDefault(preset =>
                string.Equals(preset.PresetId, SelectedPresetId, StringComparison.OrdinalIgnoreCase)
            );
}
