namespace ArcNET.Editor;

/// <summary>
/// Named prefab-like object placement preset that can be applied through the existing placement-set pipeline.
/// </summary>
public sealed class EditorObjectPalettePlacementPreset
{
    /// <summary>
    /// Stable host-facing identifier for the preset.
    /// </summary>
    public required string PresetId { get; init; }

    /// <summary>
    /// Display name for the preset.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description presented by hosts when browsing presets.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Stable placement entries applied in order for each target tile.
    /// </summary>
    public required IReadOnlyList<EditorObjectPalettePlacementRequest> Entries { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the preset contains one or more placement entries.
    /// </summary>
    public bool HasEntries => Entries.Count > 0;

    /// <summary>
    /// Converts this preset into one reusable placement set for session application.
    /// </summary>
    public EditorObjectPalettePlacementSet CreatePlacementSet() => new() { Name = Name, Entries = Entries };

    /// <summary>
    /// Creates one named placement preset from the supplied entries.
    /// </summary>
    public static EditorObjectPalettePlacementPreset Create(
        string presetId,
        string name,
        string? description = null,
        params EditorObjectPalettePlacementRequest[] entries
    ) =>
        new()
        {
            PresetId = presetId,
            Name = name,
            Description = description,
            Entries = entries,
        };
}
