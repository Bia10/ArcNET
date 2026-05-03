using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// Host-facing selected-object snapshot for one tracked map view.
/// </summary>
public sealed class EditorMapObjectSelectionSummary
{
    /// <summary>
    /// Stable identifier of the tracked map-view state that owns this selection snapshot.
    /// </summary>
    public required string MapViewStateId { get; init; }

    /// <summary>
    /// Map name targeted by the owning map view.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// Normalized persisted selection state.
    /// </summary>
    public required EditorProjectMapSelectionState Selection { get; init; }

    /// <summary>
    /// Resolved preview objects currently targeted by the persisted selection.
    /// </summary>
    public required IReadOnlyList<EditorMapObjectPreview> SelectedObjects { get; init; }

    /// <summary>
    /// Explicit selected object identifiers that did not currently resolve from the staged scene preview.
    /// </summary>
    public required IReadOnlyList<GameObjectGuid> MissingObjectIds { get; init; }

    /// <summary>
    /// Sector asset paths touched by the resolved object selection in stable order.
    /// </summary>
    public required IReadOnlyList<string> SectorAssetPaths { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when one or more objects are currently resolved by the persisted selection.
    /// </summary>
    public bool HasResolvedObjects => SelectedObjects.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when one or more resolved objects can be edited through tracked selection helpers.
    /// </summary>
    public bool CanApplyTrackedEdit => SelectedObjects.Count > 0;
}
