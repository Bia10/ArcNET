namespace ArcNET.Editor;

/// <summary>
/// One sector placed into a projected map grid.
/// </summary>
public sealed class EditorMapSectorProjection
{
    /// <summary>
    /// Sector summary that owns the projected placement.
    /// </summary>
    public required EditorSectorSummary Sector { get; init; }

    /// <summary>
    /// Defining asset for the projected sector.
    /// </summary>
    public EditorAssetEntry Asset => Sector.Asset;

    /// <summary>
    /// Absolute sector-grid X coordinate encoded by the source asset path.
    /// </summary>
    public required int SectorX { get; init; }

    /// <summary>
    /// Absolute sector-grid Y coordinate encoded by the source asset path.
    /// </summary>
    public required int SectorY { get; init; }

    /// <summary>
    /// Zero-based X coordinate in this map projection's dense local grid.
    /// </summary>
    public required int LocalX { get; init; }

    /// <summary>
    /// Zero-based Y coordinate in this map projection's dense local grid.
    /// </summary>
    public required int LocalY { get; init; }

    /// <summary>
    /// Map-local object density band derived from <see cref="EditorSectorSummary.ObjectCount"/>.
    /// </summary>
    public required EditorMapSectorDensityBand ObjectDensityBand { get; init; }

    /// <summary>
    /// Map-local blocked-tile density band derived from <see cref="EditorSectorSummary.BlockedTileCount"/>.
    /// </summary>
    public required EditorMapSectorDensityBand BlockedTileDensityBand { get; init; }

    /// <summary>
    /// Normalized preview traits derived from the projected sector summary.
    /// </summary>
    public EditorMapSectorPreviewFlags PreviewFlags =>
        EditorMapSectorPreviewFlags.Occupied
        | (Sector.HasRoofs ? EditorMapSectorPreviewFlags.HasRoofs : EditorMapSectorPreviewFlags.None)
        | (Sector.LightCount > 0 ? EditorMapSectorPreviewFlags.HasLights : EditorMapSectorPreviewFlags.None)
        | (Sector.BlockedTileCount > 0 ? EditorMapSectorPreviewFlags.HasBlockedTiles : EditorMapSectorPreviewFlags.None)
        | (
            Sector.SectorScriptId is not null || Sector.TileScriptCount > 0
                ? EditorMapSectorPreviewFlags.HasScripts
                : EditorMapSectorPreviewFlags.None
        );
}
