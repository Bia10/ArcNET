using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// Typed map-selection state persisted with one project map view.
/// </summary>
public sealed class EditorProjectMapSelectionState
{
    /// <summary>
    /// Optional normalized sector asset path currently selected in the map view.
    /// </summary>
    public string? SectorAssetPath { get; init; }

    /// <summary>
    /// Optional currently selected tile coordinate.
    /// </summary>
    public Location? Tile { get; init; }

    /// <summary>
    /// Optional currently selected object identifier.
    /// </summary>
    public GameObjectGuid? ObjectId { get; init; }

    /// <summary>
    /// Optional map-local drag-box or area selection bounds.
    /// </summary>
    public EditorProjectMapAreaSelectionState? Area { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the selection carries either one local tile or one map-local area.
    /// </summary>
    public bool HasTileSelection => Tile is not null || Area is not null;

    /// <summary>
    /// Returns <see langword="true"/> when the selection carries one persisted drag-box or area selection.
    /// </summary>
    public bool HasAreaSelection => Area is not null;

    /// <summary>
    /// Returns the explicit object count represented by this selection.
    /// </summary>
    public int SelectedObjectCount =>
        Area is { ObjectIds.Count: > 0 } ? Area.ObjectIds.Count
        : ObjectId is null ? 0
        : 1;

    /// <summary>
    /// Returns <see langword="true"/> when the selection names one or more objects.
    /// </summary>
    public bool HasObjectSelection => SelectedObjectCount > 0;

    /// <summary>
    /// Returns <see langword="true"/> when the selection names more than one object.
    /// </summary>
    public bool HasMultipleObjectSelection => SelectedObjectCount > 1;

    /// <summary>
    /// Returns the explicit object identities represented by this selection.
    /// </summary>
    public IReadOnlyList<GameObjectGuid> GetSelectedObjectIds()
    {
        if (Area is { ObjectIds.Count: > 0 } area)
            return area.ObjectIds;

        return ObjectId is { } objectId ? [objectId] : [];
    }
}
