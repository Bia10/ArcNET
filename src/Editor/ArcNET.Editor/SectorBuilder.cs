using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Fluent mutable builder for <see cref="Sector"/> instances.
/// Construct from an existing sector to edit it, or from the parameterless constructor
/// to start with an empty sector (zero-art tiles, no lights, no objects, empty block mask).
/// Call <see cref="Build"/> to produce an immutable <see cref="Sector"/>.
/// </summary>
public sealed class SectorBuilder
{
    private const int TilesPerSectorAxis = 64;
    private const int RoofsPerSectorAxis = 16;

    private readonly List<SectorLight> _lights;
    private readonly uint[] _tiles;
    private bool _hasRoofs;
    private uint[]? _roofs;
    private GameObjectScript? _sectorScript;
    private readonly List<TileScript> _tileScripts;
    private int _townmapInfo;
    private int _aptitudeAdjustment;
    private int _lightSchemeIdx;
    private SectorSoundList _soundList;
    private readonly uint[] _blockMask;
    private readonly List<MobData> _objects;

    /// <summary>
    /// Initialises an empty sector builder with zero-art tiles and silent sound configuration.
    /// </summary>
    public SectorBuilder()
    {
        _lights = [];
        _tiles = new uint[TilesPerSectorAxis * TilesPerSectorAxis];
        _hasRoofs = false;
        _roofs = null;
        _sectorScript = null;
        _tileScripts = [];
        _townmapInfo = 0;
        _aptitudeAdjustment = 0;
        _lightSchemeIdx = 0;
        _soundList = SectorSoundList.Default;
        _blockMask = new uint[TilesPerSectorAxis * TilesPerSectorAxis / 32];
        _objects = [];
    }

    /// <summary>
    /// Initialises a builder pre-populated with all data from <paramref name="sector"/>.
    /// </summary>
    public SectorBuilder(Sector sector)
    {
        _lights = new List<SectorLight>(sector.Lights);
        _tiles = (uint[])sector.Tiles.Clone();
        _hasRoofs = sector.HasRoofs;
        _roofs = sector.Roofs is not null ? (uint[])sector.Roofs.Clone() : null;
        _sectorScript = sector.SectorScript;
        _tileScripts = new List<TileScript>(sector.TileScripts);
        _townmapInfo = sector.TownmapInfo;
        _aptitudeAdjustment = sector.AptitudeAdjustment;
        _lightSchemeIdx = sector.LightSchemeIdx;
        _soundList = sector.SoundList;
        _blockMask = (uint[])sector.BlockMask.Clone();
        _objects = new List<MobData>(sector.Objects);
    }

    // ── Lights ────────────────────────────────────────────────────────────────

    /// <summary>Appends a light source to the sector.</summary>
    public SectorBuilder AddLight(SectorLight light)
    {
        _lights.Add(light);
        return this;
    }

    /// <summary>Removes the light at <paramref name="index"/>.</summary>
    public SectorBuilder RemoveLight(int index)
    {
        _lights.RemoveAt(index);
        return this;
    }

    /// <summary>Removes all lights from the sector.</summary>
    public SectorBuilder ClearLights()
    {
        _lights.Clear();
        return this;
    }

    // ── Tiles ─────────────────────────────────────────────────────────────────

    /// <summary>Sets the art ID for tile (<paramref name="tileX"/>, <paramref name="tileY"/>).</summary>
    public SectorBuilder SetTile(int tileX, int tileY, uint artId)
    {
        _tiles[tileY * TilesPerSectorAxis + tileX] = artId;
        return this;
    }

    // ── Roofs ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the roof art ID for roof cell (<paramref name="roofX"/>, <paramref name="roofY"/>).
    /// Calling this method enables <see cref="Sector.HasRoofs"/> automatically.
    /// </summary>
    public SectorBuilder SetRoof(int roofX, int roofY, uint artId)
    {
        _hasRoofs = true;
        _roofs ??= new uint[RoofsPerSectorAxis * RoofsPerSectorAxis];
        _roofs[roofY * RoofsPerSectorAxis + roofX] = artId;
        return this;
    }

    /// <summary>Removes all roof data from the sector.</summary>
    public SectorBuilder ClearRoofs()
    {
        _hasRoofs = false;
        _roofs = null;
        return this;
    }

    // ── Block mask ────────────────────────────────────────────────────────────

    /// <summary>Marks tile (<paramref name="tileX"/>, <paramref name="tileY"/>) as blocked or unblocked.</summary>
    public SectorBuilder SetBlocked(int tileX, int tileY, bool blocked)
    {
        _blockMask.SetBlocked(tileX, tileY, blocked);
        return this;
    }

    // ── Objects ───────────────────────────────────────────────────────────────

    /// <summary>Appends a static object to the sector.</summary>
    public SectorBuilder AddObject(MobData obj)
    {
        _objects.Add(obj);
        return this;
    }

    /// <summary>Removes the object at <paramref name="index"/>.</summary>
    public SectorBuilder RemoveObject(int index)
    {
        _objects.RemoveAt(index);
        return this;
    }

    /// <summary>Removes all objects from the sector.</summary>
    public SectorBuilder ClearObjects()
    {
        _objects.Clear();
        return this;
    }

    // ── Tile scripts ──────────────────────────────────────────────────────────

    /// <summary>Appends a per-tile script entry.</summary>
    public SectorBuilder AddTileScript(TileScript script)
    {
        _tileScripts.Add(script);
        return this;
    }

    /// <summary>Removes the tile script at <paramref name="index"/>.</summary>
    public SectorBuilder RemoveTileScript(int index)
    {
        _tileScripts.RemoveAt(index);
        return this;
    }

    // ── Metadata ──────────────────────────────────────────────────────────────

    /// <summary>Sets the sector-level script (pass <see langword="null"/> to clear).</summary>
    public SectorBuilder WithSectorScript(GameObjectScript? script)
    {
        _sectorScript = script;
        return this;
    }

    /// <summary>Sets the background sound configuration.</summary>
    public SectorBuilder WithSoundList(SectorSoundList soundList)
    {
        _soundList = soundList;
        return this;
    }

    /// <summary>Sets the townmap display flag.</summary>
    public SectorBuilder WithTownmapInfo(int value)
    {
        _townmapInfo = value;
        return this;
    }

    /// <summary>Sets the encounter aptitude adjustment.</summary>
    public SectorBuilder WithAptitudeAdjustment(int value)
    {
        _aptitudeAdjustment = value;
        return this;
    }

    /// <summary>Sets the light scheme index.</summary>
    public SectorBuilder WithLightSchemeIdx(int value)
    {
        _lightSchemeIdx = value;
        return this;
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Produces an immutable <see cref="Sector"/> from the current builder state.
    /// The returned sector is independent of this builder — further mutations do not affect it.
    /// </summary>
    public Sector Build() =>
        new()
        {
            // Snapshot all mutable collections so further builder mutations cannot affect the returned sector.
            Lights = new List<SectorLight>(_lights).AsReadOnly(),
            Tiles = (uint[])_tiles.Clone(),
            HasRoofs = _hasRoofs,
            Roofs = _roofs is not null ? (uint[])_roofs.Clone() : null,
            SectorScript = _sectorScript,
            TileScripts = new List<TileScript>(_tileScripts).AsReadOnly(),
            TownmapInfo = _townmapInfo,
            AptitudeAdjustment = _aptitudeAdjustment,
            LightSchemeIdx = _lightSchemeIdx,
            SoundList = _soundList,
            BlockMask = (uint[])_blockMask.Clone(),
            Objects = new List<MobData>(_objects).AsReadOnly(),
        };
}
