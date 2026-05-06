using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// One resolved map-focus target that a host can open and center inside the editor.
/// </summary>
public sealed class EditorMapFocusTarget
{
    /// <summary>
    /// Original user-facing query text that resolved this target.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Map name that owns the resolved target.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// Sector asset path that contains the resolved tile or object.
    /// </summary>
    public required string SectorAssetPath { get; init; }

    /// <summary>
    /// Sector-local tile coordinate that should be selected.
    /// </summary>
    public required Location Tile { get; init; }

    /// <summary>
    /// Map-local camera center X in tile coordinates.
    /// </summary>
    public required double CenterTileX { get; init; }

    /// <summary>
    /// Map-local camera center Y in tile coordinates.
    /// </summary>
    public required double CenterTileY { get; init; }

    /// <summary>
    /// Optional selected object identifier when the target resolves one placed object.
    /// </summary>
    public GameObjectGuid? ObjectId { get; init; }

    /// <summary>
    /// Optional proto number for the resolved object or query.
    /// </summary>
    public int? ProtoNumber { get; init; }

    /// <summary>
    /// Asset path that should be opened and focused in the host when available.
    /// For proto queries this is the proto definition; for mob and sector queries it is the backing asset.
    /// </summary>
    public string? FocusAssetPath { get; init; }

    /// <summary>
    /// Backing asset path that directly owns the resolved placement.
    /// </summary>
    public string? SourceAssetPath { get; init; }

    /// <summary>
    /// Total number of focusable map matches discovered for the query.
    /// When greater than one, this target is the first stable match.
    /// </summary>
    public int MatchCount { get; init; } = 1;
}
