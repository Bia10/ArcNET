namespace ArcNET.Editor;

/// <summary>
/// Host-facing terrain palette browser summary for one tracked map view.
/// </summary>
public sealed class EditorMapTerrainPaletteSummary
{
    /// <summary>
    /// Stable identifier of the tracked map-view state that owns this palette snapshot.
    /// </summary>
    public required string MapViewStateId { get; init; }

    /// <summary>
    /// Map name targeted by the owning map view.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// Normalized persisted terrain-tool state that backs the current selection.
    /// </summary>
    public required EditorProjectMapTerrainToolState ToolState { get; init; }

    /// <summary>
    /// Map-properties asset path currently used for terrain palette browsing.
    /// </summary>
    public required string MapPropertiesAssetPath { get; init; }

    /// <summary>
    /// Palette entries currently exposed for browser binding.
    /// </summary>
    public required IReadOnlyList<EditorTerrainPaletteEntry> Entries { get; init; }

    /// <summary>
    /// Currently selected terrain palette entry, or <see langword="null"/> when no tracked selection resolves.
    /// </summary>
    public EditorTerrainPaletteEntry? SelectedEntry { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when one or more palette entries are available for browsing.
    /// </summary>
    public bool CanBrowse => Entries.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when the tracked selection currently resolves to one browseable entry.
    /// </summary>
    public bool HasSelectedEntry => SelectedEntry is not null;
}
