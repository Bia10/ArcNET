namespace ArcNET.Editor;

/// <summary>
/// Host-facing snapshot of the tracked object-placement tool for one map view.
/// </summary>
public sealed class EditorMapObjectPlacementToolSummary
{
    /// <summary>
    /// Stable identifier of the tracked map-view state that owns this tool snapshot.
    /// </summary>
    public required string MapViewStateId { get; init; }

    /// <summary>
    /// Map name targeted by the owning map view.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// Normalized persisted object-placement tool state.
    /// </summary>
    public required EditorProjectMapObjectPlacementToolState ToolState { get; init; }

    /// <summary>
    /// Effective placement set represented by the current tool mode, or <see langword="null"/> when unresolved.
    /// </summary>
    public EditorObjectPalettePlacementSet? EffectivePlacementSet { get; init; }

    /// <summary>
    /// Selected preset resolved from the persisted library when the tool is in preset mode.
    /// </summary>
    public EditorObjectPalettePlacementPreset? SelectedPreset { get; init; }

    /// <summary>
    /// Palette entries resolved from the effective placement requests in stable request order.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> ResolvedPaletteEntries { get; init; } = [];

    /// <summary>
    /// Proto numbers referenced by the effective placement requests that do not currently resolve to loaded palette entries.
    /// </summary>
    public IReadOnlyList<int> MissingProtoNumbers { get; init; } = [];

    /// <summary>
    /// Returns <see langword="true"/> when the tracked tool has one fully resolved effective placement set.
    /// </summary>
    public bool CanPreviewOrApply => EffectivePlacementSet is { HasEntries: true } && MissingProtoNumbers.Count == 0;
}
