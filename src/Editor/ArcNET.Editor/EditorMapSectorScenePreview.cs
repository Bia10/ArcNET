using System.Numerics;

namespace ArcNET.Editor;

/// <summary>
/// Rich preview data for one positioned sector inside a projected map.
/// </summary>
public sealed class EditorMapSectorScenePreview
{
    private const int TileGridWidth = 64;
    private const int TileGridHeight = 64;
    private const int RoofGridWidth = 16;
    private const int RoofGridHeight = 16;

    /// <summary>
    /// Defining sector asset path.
    /// </summary>
    public required string AssetPath { get; init; }

    /// <summary>
    /// Absolute sector-grid X coordinate.
    /// </summary>
    public required int SectorX { get; init; }

    /// <summary>
    /// Absolute sector-grid Y coordinate.
    /// </summary>
    public required int SectorY { get; init; }

    /// <summary>
    /// Dense local-grid X coordinate in the parent map preview.
    /// </summary>
    public required int LocalX { get; init; }

    /// <summary>
    /// Dense local-grid Y coordinate in the parent map preview.
    /// </summary>
    public required int LocalY { get; init; }

    /// <summary>
    /// Normalized preview traits for the sector.
    /// </summary>
    public required EditorMapSectorPreviewFlags PreviewFlags { get; init; }

    /// <summary>
    /// Map-local object density band.
    /// </summary>
    public required EditorMapSectorDensityBand ObjectDensityBand { get; init; }

    /// <summary>
    /// Map-local blocked-tile density band.
    /// </summary>
    public required EditorMapSectorDensityBand BlockedTileDensityBand { get; init; }

    /// <summary>
    /// Raw sector tile art identifiers in 64x64 row-major order.
    /// </summary>
    public required uint[] TileArtIds { get; init; }

    /// <summary>
    /// Raw sector roof art identifiers in 16x16 row-major order when the sector has roofs.
    /// </summary>
    public uint[]? RoofArtIds { get; init; }

    /// <summary>
    /// Raw 4096-bit blocked-tile mask stored as 128 uint32 values.
    /// </summary>
    public required uint[] BlockMask { get; init; }

    /// <summary>
    /// Preview-ready light markers inside the sector.
    /// </summary>
    public required IReadOnlyList<EditorMapLightPreview> Lights { get; init; }

    /// <summary>
    /// Preview-ready tile-script markers inside the sector.
    /// </summary>
    public required IReadOnlyList<EditorMapTileScriptPreview> TileScripts { get; init; }

    /// <summary>
    /// Preview-ready placed-object markers inside the sector.
    /// </summary>
    public required IReadOnlyList<EditorMapObjectPreview> Objects { get; init; }

    /// <summary>
    /// Sector tile-grid width.
    /// </summary>
    public int TileWidth => TileGridWidth;

    /// <summary>
    /// Sector tile-grid height.
    /// </summary>
    public int TileHeight => TileGridHeight;

    /// <summary>
    /// Sector roof-grid width.
    /// </summary>
    public int RoofWidth => RoofGridWidth;

    /// <summary>
    /// Sector roof-grid height.
    /// </summary>
    public int RoofHeight => RoofGridHeight;

    /// <summary>
    /// Precomputed dense-tile row bitmasks. Each <see cref="ulong"/> bit represents one column.
    /// A zero row can be skipped entirely during iteration.
    /// </summary>
    public ulong[] TileRowMasks
    {
        get
        {
            if (_tileRowMasks is not null)
                return _tileRowMasks;

            var masks = new ulong[TileGridHeight];
            for (var row = 0; row < TileGridHeight; row++)
            {
                var rowMask = 0UL;
                for (var col = 0; col < TileGridWidth; col++)
                {
                    if (TileArtIds[(row * TileGridWidth) + col] != 0)
                        rowMask |= 1UL << col;
                }

                masks[row] = rowMask;
            }

            _tileRowMasks = masks;
            return masks;
        }
    }

    /// <summary>
    /// Precomputed fast-lookup set of tile indices that carry a light.
    /// Computed once on first access, avoiding per-sector LINQ allocations during floor-render iteration.
    /// </summary>
    public HashSet<int> LightTileIndices
    {
        get
        {
            if (_lightTileIndices is not null)
                return _lightTileIndices;

            var indices = new HashSet<int>(Lights.Count);
            for (var i = 0; i < Lights.Count; i++)
            {
                var light = Lights[i];
                indices.Add((light.TileY * TileGridWidth) + light.TileX);
            }

            _lightTileIndices = indices;
            return indices;
        }
    }

    /// <summary>
    /// Precomputed fast-lookup set of tile indices that carry a script.
    /// Computed once on first access, avoiding per-sector LINQ allocations during floor-render iteration.
    /// </summary>
    public HashSet<int> ScriptedTileIndices
    {
        get
        {
            if (_scriptedTileIndices is not null)
                return _scriptedTileIndices;

            var indices = new HashSet<int>(TileScripts.Count);
            for (var i = 0; i < TileScripts.Count; i++)
                indices.Add(TileScripts[i].TileIndex);

            _scriptedTileIndices = indices;
            return indices;
        }
    }

    /// <summary>
    /// Precomputed roof-tile row bitmasks. Each <see cref="ulong"/> bit represents one column.
    /// A zero row can be skipped entirely during iteration. Returns <see langword="null"/> when the sector has no roofs.
    /// </summary>
    public ulong[]? RoofRowMasks
    {
        get
        {
            if (RoofArtIds is null)
                return null;

            if (_roofRowMasks is not null)
                return _roofRowMasks;

            var masks = new ulong[RoofGridHeight];
            for (var row = 0; row < RoofGridHeight; row++)
            {
                var rowMask = 0UL;
                for (var col = 0; col < RoofGridWidth; col++)
                {
                    var artId = RoofArtIds[(row * RoofGridWidth) + col];
                    if (artId is not (0u or uint.MaxValue))
                        rowMask |= 1UL << col;
                }

                masks[row] = rowMask;
            }

            _roofRowMasks = masks;
            return masks;
        }
    }

    private ulong[]? _tileRowMasks;
    private ulong[]? _roofRowMasks;
    private HashSet<int>? _lightTileIndices;
    private HashSet<int>? _scriptedTileIndices;

    /// <summary>
    /// Returns one tile art identifier from the 64x64 tile grid.
    /// </summary>
    public uint GetTileArtId(int tileX, int tileY)
    {
        ValidateTileCoordinates(tileX, tileY);
        return TileArtIds[(tileY * TileGridWidth) + tileX];
    }

    /// <summary>
    /// Returns one roof art identifier from the 16x16 roof grid, or <see langword="null"/> when no roof layer exists.
    /// </summary>
    public uint? GetRoofArtId(int roofX, int roofY)
    {
        if (RoofArtIds is null)
            return null;

        ValidateRoofCoordinates(roofX, roofY);
        return RoofArtIds[(roofY * RoofGridWidth) + roofX];
    }

    /// <summary>
    /// Returns <see langword="true"/> when the supplied tile is blocked in the raw sector mask.
    /// </summary>
    public bool IsTileBlocked(int tileX, int tileY)
    {
        ValidateTileCoordinates(tileX, tileY);

        var tileIndex = (tileY * TileGridWidth) + tileX;
        return (BlockMask[tileIndex / 32] & (1u << (tileIndex % 32))) != 0;
    }

    private static void ValidateTileCoordinates(int tileX, int tileY)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tileX);
        ArgumentOutOfRangeException.ThrowIfNegative(tileY);

        if (tileX >= TileGridWidth)
            throw new ArgumentOutOfRangeException(
                nameof(tileX),
                tileX,
                $"Tile X must be between 0 and {TileGridWidth - 1}."
            );

        if (tileY >= TileGridHeight)
            throw new ArgumentOutOfRangeException(
                nameof(tileY),
                tileY,
                $"Tile Y must be between 0 and {TileGridHeight - 1}."
            );
    }

    private static void ValidateRoofCoordinates(int roofX, int roofY)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(roofX);
        ArgumentOutOfRangeException.ThrowIfNegative(roofY);

        if (roofX >= RoofGridWidth)
            throw new ArgumentOutOfRangeException(
                nameof(roofX),
                roofX,
                $"Roof X must be between 0 and {RoofGridWidth - 1}."
            );

        if (roofY >= RoofGridHeight)
            throw new ArgumentOutOfRangeException(
                nameof(roofY),
                roofY,
                $"Roof Y must be between 0 and {RoofGridHeight - 1}."
            );
    }
}
