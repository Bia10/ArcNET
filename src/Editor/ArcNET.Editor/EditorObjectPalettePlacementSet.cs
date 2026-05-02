namespace ArcNET.Editor;

/// <summary>
/// Reusable palette-driven placement set that stamps multiple configured object presets per target tile.
/// </summary>
public sealed class EditorObjectPalettePlacementSet
{
    /// <summary>
    /// Optional host-facing display name for the placement set.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Stable placement entries applied in order for each target tile.
    /// </summary>
    public required IReadOnlyList<EditorObjectPalettePlacementRequest> Entries { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the set contains one or more placement entries.
    /// </summary>
    public bool HasEntries => Entries.Count > 0;

    /// <summary>
    /// Creates one placement set from the supplied placement entries.
    /// </summary>
    public static EditorObjectPalettePlacementSet Create(
        string? name = null,
        params EditorObjectPalettePlacementRequest[] entries
    ) => new() { Name = name, Entries = entries };
}
