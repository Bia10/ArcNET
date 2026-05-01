using ArcNET.Core.Primitives;
using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Result of applying one object-brush request to grouped scene-sector hits.
/// </summary>
public sealed class EditorMapObjectBrushResult
{
    /// <summary>
    /// Objects created by a stamp or replace request, in stable grouped-hit order.
    /// </summary>
    public IReadOnlyList<MobData> CreatedObjects { get; init; } = [];

    /// <summary>
    /// Object IDs removed by an erase or replace request, in stable grouped-hit order.
    /// </summary>
    public IReadOnlyList<GameObjectGuid> RemovedObjectIds { get; init; } = [];

    /// <summary>
    /// Number of created objects in <see cref="CreatedObjects"/>.
    /// </summary>
    public int CreatedObjectCount => CreatedObjects.Count;

    /// <summary>
    /// Number of removed objects in <see cref="RemovedObjectIds"/>.
    /// </summary>
    public int RemovedObjectCount => RemovedObjectIds.Count;

    /// <summary>
    /// Returns <see langword="true"/> when the request staged one or more object changes.
    /// </summary>
    public bool HasChanges => CreatedObjectCount > 0 || RemovedObjectCount > 0;
}
