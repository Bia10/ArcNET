using System.Numerics;
using ArcNET.Core.Primitives;

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
    private static readonly ArtId[] EmptyArtIds = [];

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
    /// CE light-scheme index stored on the sector.
    /// A value of zero resolves to the map default light scheme.
    /// </summary>
    public int LightSchemeIdx { get; init; }

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
    /// Preview-ready jump-point markers inside the sector.
    /// </summary>
    public IReadOnlyList<EditorMapJumpPointPreview> JumpPoints { get; init; } = [];

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
    /// Precomputed fast-lookup set of tile indices that carry one or more jump points.
    /// Computed once on first access, avoiding per-sector LINQ allocations during floor-render iteration.
    /// </summary>
    public HashSet<int> JumpPointTileIndices
    {
        get
        {
            if (_jumpPointTileIndices is not null)
                return _jumpPointTileIndices;

            var indices = new HashSet<int>(JumpPoints.Count);
            for (var i = 0; i < JumpPoints.Count; i++)
                indices.Add(JumpPoints[i].TileIndex);

            _jumpPointTileIndices = indices;
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

    /// <summary>
    /// Unique non-empty floor tile art ids referenced by this sector's terrain.
    /// </summary>
    public IReadOnlyList<ArtId> UniqueTerrainFloorArtIds =>
        _uniqueTerrainFloorArtIds ??= CreateUniqueTerrainArtIds(
            TileArtIds,
            skipRoofSentinels: false,
            skipRoofFill: false
        );

    /// <summary>
    /// Unique drawable roof art ids referenced by this sector's terrain.
    /// </summary>
    public IReadOnlyList<ArtId> UniqueTerrainRoofArtIds =>
        _uniqueTerrainRoofArtIds ??= RoofArtIds is null
            ? EmptyArtIds
            : CreateUniqueTerrainArtIds(RoofArtIds, skipRoofSentinels: true, skipRoofFill: true, skipRoofFaded: true);

    /// <summary>
    /// Unique sector-light art ids referenced by this sector's terrain.
    /// </summary>
    public IReadOnlyList<ArtId> UniqueTerrainLightArtIds =>
        _uniqueTerrainLightArtIds ??= CreateUniqueTerrainLightArtIds();

    private ulong[]? _tileRowMasks;
    private ulong[]? _roofRowMasks;
    private HashSet<int>? _lightTileIndices;
    private HashSet<int>? _scriptedTileIndices;
    private HashSet<int>? _jumpPointTileIndices;
    private ArtId[]? _uniqueTerrainFloorArtIds;
    private ArtId[]? _uniqueTerrainRoofArtIds;
    private ArtId[]? _uniqueTerrainLightArtIds;
    private long? _terrainRevision;

    /// <summary>
    /// Stable terrain-only revision for chunk-local virtual terrain rendering.
    /// </summary>
    public long TerrainRevision => _terrainRevision ??= ComputeTerrainRevision();

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

    private static ArtId[] CreateUniqueTerrainArtIds(
        uint[] artIds,
        bool skipRoofSentinels,
        bool skipRoofFill,
        bool skipRoofFaded = false
    )
    {
        var uniqueValues = new HashSet<uint>();
        for (var index = 0; index < artIds.Length; index++)
        {
            var artId = artIds[index];
            if (artId == 0 || (skipRoofSentinels && artId == uint.MaxValue))
                continue;

            var typedArtId = new ArtId(artId);
            if (skipRoofFill && typedArtId.IsRoofFill)
                continue;

            if (skipRoofFaded && typedArtId.IsRoofFaded)
                continue;

            uniqueValues.Add(artId);
        }

        return CreateSortedArtIds(uniqueValues);
    }

    private ArtId[] CreateUniqueTerrainLightArtIds()
    {
        var uniqueValues = new HashSet<uint>();
        for (var index = 0; index < Lights.Count; index++)
        {
            var artId = Lights[index].ArtId.Value;
            if (artId != 0)
                uniqueValues.Add(artId);
        }

        return CreateSortedArtIds(uniqueValues);
    }

    private static ArtId[] CreateSortedArtIds(HashSet<uint> uniqueValues)
    {
        if (uniqueValues.Count == 0)
            return EmptyArtIds;

        var artIds = new ArtId[uniqueValues.Count];
        var index = 0;
        foreach (var value in uniqueValues)
            artIds[index++] = new ArtId(value);

        Array.Sort(artIds, static (a, b) => a.Value.CompareTo(b.Value));
        return artIds;
    }

    private long ComputeTerrainRevision()
    {
        var hash = new StableTerrainRevisionHash();
        hash.Add(AssetPath);
        hash.Add(SectorX);
        hash.Add(SectorY);
        hash.Add(LocalX);
        hash.Add(LocalY);
        hash.Add(LightSchemeIdx);

        hash.Add(TileArtIds.Length);
        for (var index = 0; index < TileArtIds.Length; index++)
            hash.Add(TileArtIds[index]);

        hash.Add(RoofArtIds?.Length ?? -1);
        if (RoofArtIds is not null)
        {
            for (var index = 0; index < RoofArtIds.Length; index++)
                hash.Add(RoofArtIds[index]);
        }

        hash.Add(BlockMask.Length);
        for (var index = 0; index < BlockMask.Length; index++)
            hash.Add(BlockMask[index]);

        hash.Add(Lights.Count);
        for (var index = 0; index < Lights.Count; index++)
        {
            var light = Lights[index];
            hash.Add(light.TileX);
            hash.Add(light.TileY);
            hash.Add(light.OffsetX);
            hash.Add(light.OffsetY);
            hash.Add(light.ArtId.Value);
            hash.Add((int)light.Flags);
            hash.Add(light.Palette);
            hash.Add(light.Red);
            hash.Add(light.Green);
            hash.Add(light.Blue);
            hash.Add(light.TintColor);
        }

        hash.Add(TileScripts.Count);
        for (var index = 0; index < TileScripts.Count; index++)
        {
            var script = TileScripts[index];
            hash.Add(script.TileIndex);
            hash.Add(script.TileX);
            hash.Add(script.TileY);
            hash.Add(script.ScriptId);
            hash.Add(script.NodeFlags);
            hash.Add(script.ScriptFlags);
            hash.Add(script.ScriptCounters);
        }

        hash.Add(JumpPoints.Count);
        for (var index = 0; index < JumpPoints.Count; index++)
        {
            var jumpPoint = JumpPoints[index];
            hash.Add(jumpPoint.TileIndex);
            hash.Add(jumpPoint.TileX);
            hash.Add(jumpPoint.TileY);
            hash.Add(jumpPoint.MapTileX);
            hash.Add(jumpPoint.MapTileY);
            hash.Add(jumpPoint.DestinationMapId);
            hash.Add(jumpPoint.DestinationTileX);
            hash.Add(jumpPoint.DestinationTileY);
            hash.Add(jumpPoint.Flags);
        }

        return hash.ToInt64();
    }

    private struct StableTerrainRevisionHash
    {
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;
        private ulong _value = OffsetBasis;

        public StableTerrainRevisionHash() { }

        public void Add(byte value) => Add((int)value);

        public void Add(int value) => Add((long)value);

        public void Add(uint value) => Add((long)value);

        public void Add(long value)
        {
            unchecked
            {
                var unsigned = (ulong)value;
                for (var index = 0; index < sizeof(ulong); index++)
                {
                    _value ^= (byte)(unsigned >> (index * 8));
                    _value *= Prime;
                }
            }
        }

        public void Add(string? value)
        {
            if (value is null)
            {
                Add(-1);
                return;
            }

            Add(value.Length);
            unchecked
            {
                foreach (var character in value)
                {
                    _value ^= (byte)character;
                    _value *= Prime;
                    _value ^= (byte)(character >> 8);
                    _value *= Prime;
                }
            }
        }

        public long ToInt64() => unchecked((long)_value);
    }
}
