namespace ArcNET.Editor;

/// <summary>
/// Host-facing object palette browser summary for one tracked map view.
/// </summary>
public sealed class EditorMapObjectPaletteSummary
{
    /// <summary>
    /// Stable identifier of the tracked map-view state that owns this browser snapshot.
    /// </summary>
    public required string MapViewStateId { get; init; }

    /// <summary>
    /// Map name targeted by the owning map view.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// Normalized persisted object-placement tool state that backs the current selection.
    /// </summary>
    public required EditorProjectMapObjectPlacementToolState ToolState { get; init; }

    /// <summary>
    /// Current free-text search applied to the browser, or <see langword="null"/> when unfiltered.
    /// </summary>
    public string? SearchText { get; init; }

    /// <summary>
    /// Current category filter applied to the browser, or <see langword="null"/> when unfiltered.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Available category names for the current browse scope in stable order.
    /// </summary>
    public required IReadOnlyList<string> AvailableCategories { get; init; }

    /// <summary>
    /// Palette entries currently exposed for browser binding.
    /// </summary>
    public required IReadOnlyList<EditorObjectPaletteEntry> Entries { get; init; }

    /// <summary>
    /// Currently selected palette entry resolved from tracked single-placement state, or <see langword="null"/>
    /// when the tracked tool is not in single-placement mode or no entry currently resolves.
    /// </summary>
    public EditorObjectPaletteEntry? SelectedEntry { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when one or more palette entries are available for browsing.
    /// </summary>
    public bool CanBrowse => Entries.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when the tracked single-placement selection currently resolves.
    /// </summary>
    public bool HasSelectedEntry => SelectedEntry is not null;
}
