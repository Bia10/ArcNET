using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Builds richer GUI-oriented map scene previews from projected sectors and loaded sector payloads.
/// </summary>
public static class EditorMapScenePreviewBuilder
{
    private const int TileGridWidth = 64;
    private const int RoofGridWidth = 16;

    /// <summary>
    /// Builds a map scene preview from a projected map and the loaded sectors that back its asset paths.
    /// </summary>
    public static EditorMapScenePreview Build(
        EditorMapProjection projection,
        IReadOnlyDictionary<string, Sector> sectorsByAssetPath
    ) => Build(projection, sectorsByAssetPath, artResolver: null);

    /// <summary>
    /// Builds a map scene preview from a projected map and the loaded sectors that back its asset paths.
    /// When <paramref name="artResolver"/> is provided, placed objects also receive conservative sprite-bounds metadata
    /// derived from the resolved ART frames.
    /// </summary>
    public static EditorMapScenePreview Build(
        EditorMapProjection projection,
        IReadOnlyDictionary<string, Sector> sectorsByAssetPath,
        Func<ArtId, ArtFile?>? artResolver
    )
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(sectorsByAssetPath);

        Dictionary<ArtId, EditorMapObjectSpriteBounds?>? spriteBoundsCache = artResolver is null ? null : new();
        var sectors = new List<EditorMapSectorScenePreview>(projection.Sectors.Count);
        foreach (var sectorProjection in projection.Sectors)
        {
            if (!sectorsByAssetPath.TryGetValue(sectorProjection.Asset.AssetPath, out var sector))
            {
                throw new InvalidOperationException(
                    $"No loaded sector payload matched '{sectorProjection.Asset.AssetPath}' for map '{projection.MapName}'."
                );
            }

            sectors.Add(BuildSector(sectorProjection, sector, artResolver, spriteBoundsCache));
        }

        return new EditorMapScenePreview
        {
            MapName = projection.MapName,
            Width = projection.Width,
            Height = projection.Height,
            UnpositionedSectorCount = projection.UnpositionedSectorCount,
            Sectors = sectors,
        };
    }

    /// <summary>
    /// Builds one sector scene preview from the projected sector metadata and the parsed sector payload.
    /// </summary>
    public static EditorMapSectorScenePreview BuildSector(EditorMapSectorProjection sectorProjection, Sector sector) =>
        BuildSector(sectorProjection, sector, artResolver: null);

    /// <summary>
    /// Builds one preview-ready object snapshot from a loaded or synthesized mob payload.
    /// </summary>
    public static EditorMapObjectPreview BuildObjectPreview(MobData mob, Func<ArtId, ArtFile?>? artResolver = null)
    {
        ArgumentNullException.ThrowIfNull(mob);

        Dictionary<ArtId, EditorMapObjectSpriteBounds?>? spriteBoundsCache = artResolver is null ? null : new();
        return BuildObject(mob, artResolver, spriteBoundsCache);
    }

    /// <summary>
    /// Builds one sector scene preview from the projected sector metadata and the parsed sector payload.
    /// When <paramref name="artResolver"/> is provided, placed objects also receive conservative sprite-bounds metadata
    /// derived from the resolved ART frames.
    /// </summary>
    public static EditorMapSectorScenePreview BuildSector(
        EditorMapSectorProjection sectorProjection,
        Sector sector,
        Func<ArtId, ArtFile?>? artResolver
    )
    {
        Dictionary<ArtId, EditorMapObjectSpriteBounds?>? spriteBoundsCache = artResolver is null ? null : new();

        return BuildSector(sectorProjection, sector, artResolver, spriteBoundsCache);
    }

    private static EditorMapSectorScenePreview BuildSector(
        EditorMapSectorProjection sectorProjection,
        Sector sector,
        Func<ArtId, ArtFile?>? artResolver,
        Dictionary<ArtId, EditorMapObjectSpriteBounds?>? spriteBoundsCache
    )
    {
        ArgumentNullException.ThrowIfNull(sectorProjection);
        ArgumentNullException.ThrowIfNull(sector);

        return new EditorMapSectorScenePreview
        {
            AssetPath = sectorProjection.Asset.AssetPath,
            SectorX = sectorProjection.SectorX,
            SectorY = sectorProjection.SectorY,
            LocalX = sectorProjection.LocalX,
            LocalY = sectorProjection.LocalY,
            PreviewFlags = sectorProjection.PreviewFlags,
            ObjectDensityBand = sectorProjection.ObjectDensityBand,
            BlockedTileDensityBand = sectorProjection.BlockedTileDensityBand,
            TileArtIds = [.. sector.Tiles],
            RoofArtIds = sector.Roofs is null ? null : [.. sector.Roofs],
            BlockMask = [.. sector.BlockMask],
            Lights = [.. sector.Lights.Select(BuildLight)],
            TileScripts = [.. sector.TileScripts.Select(BuildTileScript)],
            Objects = [.. sector.Objects.Select(mob => BuildObject(mob, artResolver, spriteBoundsCache))],
        };
    }

    private static EditorMapLightPreview BuildLight(SectorLight light) =>
        new()
        {
            TileX = light.TileX,
            TileY = light.TileY,
            OffsetX = light.OffsetX,
            OffsetY = light.OffsetY,
            ArtId = new ArtId(light.ArtId),
            Flags = light.Flags,
            Palette = light.Palette,
            Red = light.R,
            Green = light.G,
            Blue = light.B,
            TintColor = light.TintColor,
        };

    private static EditorMapTileScriptPreview BuildTileScript(TileScript tileScript)
    {
        var tileIndex = checked((int)tileScript.TileId);
        return new EditorMapTileScriptPreview
        {
            TileIndex = tileIndex,
            TileX = tileIndex % TileGridWidth,
            TileY = tileIndex / TileGridWidth,
            ScriptId = tileScript.ScriptNum,
            NodeFlags = tileScript.NodeFlags,
            ScriptFlags = tileScript.ScriptFlags,
            ScriptCounters = tileScript.ScriptCounters,
        };
    }

    private static EditorMapObjectPreview BuildObject(
        MobData mob,
        Func<ArtId, ArtFile?>? artResolver,
        Dictionary<ArtId, EditorMapObjectSpriteBounds?>? spriteBoundsCache
    )
    {
        var currentArtId = GetArtIdOrDefault(mob, ObjectField.ObjFCurrentAid);
        var location = GetPreviewLocation(mob.GetProperty(ObjectField.ObjFLocation));
        var offsetX = GetInt32OrDefault(mob, ObjectField.ObjFOffsetX);
        var offsetY = GetInt32OrDefault(mob, ObjectField.ObjFOffsetY);
        var offsetZ = GetFloatOrDefault(mob, ObjectField.ObjFOffsetZ);
        var collisionHeight = GetFloatOrDefault(mob, ObjectField.ObjFHeight);
        var rotation = GetFloatOrDefault(mob, ObjectField.ObjFPadIas1);
        var rotationPitch = GetFloatOrDefault(mob, ObjectField.ObjFRotationPitch);

        return new EditorMapObjectPreview
        {
            ObjectId = mob.Header.ObjectId,
            ProtoId = mob.Header.ProtoId,
            ObjectType = mob.Header.GameObjectType,
            CurrentArtId = currentArtId,
            Location = location,
            OffsetX = offsetX,
            OffsetY = offsetY,
            OffsetZ = offsetZ,
            CollisionHeight = collisionHeight,
            SpriteBounds = ResolveSpriteBounds(currentArtId, artResolver, spriteBoundsCache),
            Rotation = rotation,
            RotationPitch = rotationPitch,
        };
    }

    private static int GetInt32OrDefault(MobData mob, ObjectField field) =>
        mob.GetProperty(field) is { ParseNote: null } property ? property.GetInt32() : 0;

    private static float GetFloatOrDefault(MobData mob, ObjectField field) =>
        mob.GetProperty(field) is { ParseNote: null } property ? property.GetFloat() : 0f;

    private static ArtId GetArtIdOrDefault(MobData mob, ObjectField field) =>
        mob.GetProperty(field) is { ParseNote: null } property
            ? new ArtId(unchecked((uint)property.GetInt32()))
            : default;

    private static Location? GetPreviewLocation(ObjectProperty? property)
    {
        if (property is null || property.ParseNote is not null)
            return null;

        var (tileX, tileY) = property.GetLocation();
        if (tileX is < short.MinValue or > short.MaxValue || tileY is < short.MinValue or > short.MaxValue)
            return null;

        return new Location((short)tileX, (short)tileY);
    }

    private static EditorMapObjectSpriteBounds? ResolveSpriteBounds(
        ArtId artId,
        Func<ArtId, ArtFile?>? artResolver,
        Dictionary<ArtId, EditorMapObjectSpriteBounds?>? spriteBoundsCache
    )
    {
        if (artResolver is null || artId.Value == 0)
            return null;

        if (spriteBoundsCache is not null && spriteBoundsCache.TryGetValue(artId, out var cachedBounds))
            return cachedBounds;

        var resolvedBounds = artResolver(artId) is { } art ? BuildSpriteBounds(art) : null;
        if (spriteBoundsCache is not null)
            spriteBoundsCache[artId] = resolvedBounds;

        return resolvedBounds;
    }

    private static EditorMapObjectSpriteBounds? BuildSpriteBounds(ArtFile art)
    {
        ArgumentNullException.ThrowIfNull(art);

        var hasFrame = false;
        var maxFrameWidth = 0;
        var maxFrameHeight = 0;
        var maxFrameCenterX = int.MinValue;
        var maxFrameCenterY = int.MinValue;

        for (var rotationIndex = 0; rotationIndex < art.Frames.Length; rotationIndex++)
        {
            var frames = art.Frames[rotationIndex];
            for (var frameIndex = 0; frameIndex < frames.Length; frameIndex++)
            {
                var header = frames[frameIndex].Header;
                hasFrame = true;
                maxFrameWidth = Math.Max(maxFrameWidth, checked((int)header.Width));
                maxFrameHeight = Math.Max(maxFrameHeight, checked((int)header.Height));
                maxFrameCenterX = Math.Max(maxFrameCenterX, header.CenterX);
                maxFrameCenterY = Math.Max(maxFrameCenterY, header.CenterY);
            }
        }

        if (!hasFrame)
            return null;

        return new EditorMapObjectSpriteBounds
        {
            MaxFrameWidth = maxFrameWidth,
            MaxFrameHeight = maxFrameHeight,
            MaxFrameCenterX = maxFrameCenterX,
            MaxFrameCenterY = maxFrameCenterY,
        };
    }
}
