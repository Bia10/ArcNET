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
    private const uint ArtTypeMask = 0xF0000000u;
    private const uint CritterArtType = 0x20000000u;
    private const uint MonsterArtType = 0xC0000000u;
    private const uint UniqueNpcArtType = 0xD0000000u;
    private const int ArtIdRotationShift = 11;

    /// <summary>
    /// Builds a map scene preview from a projected map and the loaded sectors that back its asset paths.
    /// </summary>
    public static EditorMapScenePreview Build(
        EditorMapProjection projection,
        IReadOnlyDictionary<string, Sector> sectorsByAssetPath
    ) =>
        Build(
            projection,
            sectorsByAssetPath,
            artResolver: null,
            currentArtIdFallbackResolver: null,
            mapMobsByAssetPath: null
        );

    /// <summary>
    /// Builds a map scene preview from a projected map and the loaded sectors that back its asset paths.
    /// When <paramref name="artResolver"/> is provided, placed objects also receive conservative sprite-bounds metadata
    /// derived from the resolved ART frames.
    /// </summary>
    public static EditorMapScenePreview Build(
        EditorMapProjection projection,
        IReadOnlyDictionary<string, Sector> sectorsByAssetPath,
        Func<ArtId, ArtFile?>? artResolver
    ) =>
        Build(
            projection,
            sectorsByAssetPath,
            artResolver,
            currentArtIdFallbackResolver: null,
            mapMobsByAssetPath: null
        );

    /// <summary>
    /// Builds a map scene preview from a projected map and the loaded sectors that back its asset paths.
    /// When <paramref name="artResolver"/> is provided, placed objects also receive conservative sprite-bounds metadata
    /// derived from the resolved ART frames.
    /// When <paramref name="currentArtIdFallbackResolver"/> is provided, scene objects may derive their current ART
    /// from external metadata such as the backing proto when the live mob omits one.
    /// </summary>
    internal static EditorMapScenePreview Build(
        EditorMapProjection projection,
        IReadOnlyDictionary<string, Sector> sectorsByAssetPath,
        Func<ArtId, ArtFile?>? artResolver,
        Func<MobData, ArtId?>? currentArtIdFallbackResolver,
        IReadOnlyDictionary<string, IReadOnlyList<MobData>>? mapMobsByAssetPath
    )
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(sectorsByAssetPath);

        Dictionary<ArtId, EditorMapObjectSpriteBounds?>? spriteBoundsCache = artResolver is null ? null : new();
        var extraObjectsBySectorAssetPath = BuildMapMobObjectPreviews(
            projection,
            sectorsByAssetPath,
            mapMobsByAssetPath,
            artResolver,
            spriteBoundsCache,
            currentArtIdFallbackResolver
        );
        var sectors = new List<EditorMapSectorScenePreview>(projection.Sectors.Count);
        foreach (var sectorProjection in projection.Sectors)
        {
            if (!sectorsByAssetPath.TryGetValue(sectorProjection.Asset.AssetPath, out var sector))
            {
                throw new InvalidOperationException(
                    $"No loaded sector payload matched '{sectorProjection.Asset.AssetPath}' for map '{projection.MapName}'."
                );
            }

            sectors.Add(
                BuildSector(
                    sectorProjection,
                    sector,
                    artResolver,
                    spriteBoundsCache,
                    currentArtIdFallbackResolver,
                    extraObjectsBySectorAssetPath.TryGetValue(sectorProjection.Asset.AssetPath, out var extraObjects)
                        ? extraObjects
                        : null
                )
            );
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
    ) =>
        BuildSector(
            sectorProjection,
            sector,
            artResolver,
            spriteBoundsCache,
            currentArtIdFallbackResolver: null,
            extraObjects: null
        );

    private static EditorMapSectorScenePreview BuildSector(
        EditorMapSectorProjection sectorProjection,
        Sector sector,
        Func<ArtId, ArtFile?>? artResolver,
        Dictionary<ArtId, EditorMapObjectSpriteBounds?>? spriteBoundsCache,
        Func<MobData, ArtId?>? currentArtIdFallbackResolver,
        IReadOnlyList<EditorMapObjectPreview>? extraObjects
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
            Objects =
            [
                .. sector.Objects.Select(mob =>
                    BuildObject(
                        mob,
                        artResolver,
                        spriteBoundsCache,
                        currentArtIdFallbackResolver,
                        locationOverride: TryGetNormalizedSectorObjectLocation(sectorProjection, mob),
                        sourceAssetPath: sectorProjection.Asset.AssetPath
                    )
                ),
                .. (extraObjects ?? []),
            ],
        };
    }

    private static Dictionary<string, List<EditorMapObjectPreview>> BuildMapMobObjectPreviews(
        EditorMapProjection projection,
        IReadOnlyDictionary<string, Sector> sectorsByAssetPath,
        IReadOnlyDictionary<string, IReadOnlyList<MobData>>? mapMobsByAssetPath,
        Func<ArtId, ArtFile?>? artResolver,
        Dictionary<ArtId, EditorMapObjectSpriteBounds?>? spriteBoundsCache,
        Func<MobData, ArtId?>? currentArtIdFallbackResolver
    )
    {
        var previewsBySectorAssetPath = new Dictionary<string, List<EditorMapObjectPreview>>(
            StringComparer.OrdinalIgnoreCase
        );
        if (mapMobsByAssetPath is null || mapMobsByAssetPath.Count == 0)
            return previewsBySectorAssetPath;

        var sectorProjectionByCoordinates = projection.Sectors.ToDictionary(static sectorProjection =>
            (sectorProjection.SectorX, sectorProjection.SectorY)
        );
        var sectorObjectGuids = CollectSectorObjectGuids(sectorsByAssetPath);
        var containedObjectGuids = CollectContainedObjectGuids(mapMobsByAssetPath);

        foreach (var (assetPath, mobs) in mapMobsByAssetPath)
        {
            foreach (var mob in mobs)
            {
                if (ShouldSkipMapMob(mob, sectorObjectGuids, containedObjectGuids))
                    continue;

                if (
                    !TryProjectMapMobLocation(
                        mob,
                        sectorProjectionByCoordinates,
                        out var sectorProjection,
                        out var location
                    )
                )
                    continue;

                if (!previewsBySectorAssetPath.TryGetValue(sectorProjection.Asset.AssetPath, out var previews))
                {
                    previews = [];
                    previewsBySectorAssetPath[sectorProjection.Asset.AssetPath] = previews;
                }

                previews.Add(
                    BuildObject(mob, artResolver, spriteBoundsCache, currentArtIdFallbackResolver, location, assetPath)
                );
            }
        }

        return previewsBySectorAssetPath;
    }

    private static HashSet<Guid> CollectSectorObjectGuids(IReadOnlyDictionary<string, Sector> sectorsByAssetPath)
    {
        var guids = new HashSet<Guid>();
        foreach (var sector in sectorsByAssetPath.Values)
        {
            foreach (var mob in sector.Objects)
            {
                if (mob.Header.ObjectId.OidType == GameObjectGuid.OidTypeGuid)
                    guids.Add(mob.Header.ObjectId.Id);
            }
        }

        return guids;
    }

    private static HashSet<Guid> CollectContainedObjectGuids(
        IReadOnlyDictionary<string, IReadOnlyList<MobData>> mapMobsByAssetPath
    )
    {
        var guids = new HashSet<Guid>();
        foreach (var mobs in mapMobsByAssetPath.Values)
        {
            foreach (var mob in mobs)
            {
                AddContainedObjectGuids(mob.GetProperty(ObjectField.ContainerInventoryListIdx), guids);
                AddContainedObjectGuids(mob.GetProperty(ObjectField.CritterInventoryListIdx), guids);
            }
        }

        return guids;
    }

    private static void AddContainedObjectGuids(ObjectProperty? property, ISet<Guid> guids)
    {
        if (property is null || property.ParseNote is not null)
            return;

        try
        {
            foreach (var (oidType, _, id) in property.GetObjectIdArrayFull())
            {
                if (oidType == GameObjectGuid.OidTypeGuid)
                    guids.Add(id);
            }
        }
        catch (InvalidOperationException) { }
    }

    private static bool ShouldSkipMapMob(
        MobData mob,
        IReadOnlySet<Guid> sectorObjectGuids,
        IReadOnlySet<Guid> containedObjectGuids
    )
    {
        if (mob.Header.ObjectId.OidType != GameObjectGuid.OidTypeGuid)
            return false;

        var objectGuid = mob.Header.ObjectId.Id;
        return sectorObjectGuids.Contains(objectGuid) || containedObjectGuids.Contains(objectGuid);
    }

    private static bool TryProjectMapMobLocation(
        MobData mob,
        IReadOnlyDictionary<(int SectorX, int SectorY), EditorMapSectorProjection> sectorProjectionByCoordinates,
        out EditorMapSectorProjection sectorProjection,
        out Location location
    )
    {
        sectorProjection = null!;
        location = default;

        var property = mob.GetProperty(ObjectField.Location);
        if (property is null || property.ParseNote is not null)
            return false;

        var (mapTileX, mapTileY) = property.GetLocation();
        var sectorX = FloorDivide(mapTileX, TileGridWidth);
        var sectorY = FloorDivide(mapTileY, TileGridWidth);
        if (!sectorProjectionByCoordinates.TryGetValue((sectorX, sectorY), out var resolvedSectorProjection))
            return false;

        sectorProjection = resolvedSectorProjection;

        location = new Location(
            checked((short)PositiveModulo(mapTileX, TileGridWidth)),
            checked((short)PositiveModulo(mapTileY, TileGridWidth))
        );
        return true;
    }

    private static Location? TryGetNormalizedSectorObjectLocation(
        EditorMapSectorProjection sectorProjection,
        MobData mob
    )
    {
        _ = sectorProjection; // Sector identity no longer needed — bitmask normalizes universally.

        if (!TryGetMapLocation(mob.GetProperty(ObjectField.Location), out var tileX, out var tileY))
            return null;

        // Normalize to sector-local using the same bitmask the engine applies in
        // tile_id_from_loc: `tile_x = LOCATION_GET_X(loc) & 0x3F`.
        // This correctly handles both tile-local (0-63) and map-absolute locations
        // and guarantees the result is always in 0-63 range.
        var localTileX = tileX & 0x3F;
        var localTileY = tileY & 0x3F;

        return new Location(checked((short)localTileX), checked((short)localTileY));
    }

    private static bool TryGetMapLocation(ObjectProperty? property, out int tileX, out int tileY)
    {
        tileX = 0;
        tileY = 0;

        if (property is null || property.ParseNote is not null)
            return false;

        try
        {
            (tileX, tileY) = property.GetLocation();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static int FloorDivide(int value, int divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;
        return remainder < 0 ? quotient - 1 : quotient;
    }

    private static int PositiveModulo(int value, int divisor)
    {
        var remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
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
        Dictionary<ArtId, EditorMapObjectSpriteBounds?>? spriteBoundsCache,
        Func<MobData, ArtId?>? currentArtIdFallbackResolver = null,
        Location? locationOverride = null,
        string? sourceAssetPath = null
    )
    {
        var currentArtId = GetArtIdOrDefault(mob, ObjectField.CurrentAid);
        if (currentArtId.Value == 0)
            currentArtId = GetArtIdOrDefault(mob, ObjectField.Aid);

        if (currentArtId.Value == 0 && currentArtIdFallbackResolver is not null)
        {
            var fallbackArtId = currentArtIdFallbackResolver(mob);
            if (fallbackArtId is { } resolvedArtId && resolvedArtId.Value != 0u)
                currentArtId = resolvedArtId;
        }

        var location = locationOverride ?? GetPreviewLocation(mob.GetProperty(ObjectField.Location));
        var offsetX = GetInt32OrDefault(mob, ObjectField.OffsetX);
        var offsetY = GetInt32OrDefault(mob, ObjectField.OffsetY);
        var offsetZ = GetFloatOrDefault(mob, ObjectField.OffsetZ);
        var collisionHeight = GetFloatOrDefault(mob, ObjectField.Height);
        var flags = GetObjectFlagsOrDefault(mob);
        var wallFlags = GetInt32OrDefault(mob, ObjectField.WallFlags);
        var sceneryFlags = GetSceneryFlagsOrDefault(mob);
        var rotation = GetRotationOrDefault(mob, currentArtId);
        var rotationIndex = GetRotationIndex(mob, currentArtId, rotation);
        var blitScale = GetBlitScaleOrDefault(mob);
        var rotationPitch = GetFloatOrDefault(mob, ObjectField.RotationPitch);
        var hpProp = mob.GetProperty(ObjectField.HpPts);
        var isDead =
            (mob.Header.GameObjectType is ObjectType.Pc or ObjectType.Npc)
            && hpProp is not null
            && hpProp.GetInt32() <= 0;

        return new EditorMapObjectPreview
        {
            ObjectId = mob.Header.ObjectId,
            ProtoId = mob.Header.ProtoId,
            ObjectType = mob.Header.GameObjectType,
            CurrentArtId = currentArtId,
            Flags = flags,
            SourceAssetPath = sourceAssetPath,
            Location = location,
            OffsetX = offsetX,
            OffsetY = offsetY,
            OffsetZ = offsetZ,
            CollisionHeight = collisionHeight,
            SpriteBounds = ResolveSpriteBounds(currentArtId, artResolver, spriteBoundsCache),
            Rotation = rotation,
            RotationIndex = rotationIndex,
            BlitScale = blitScale,
            RotationPitch = rotationPitch,
            WallFlags = wallFlags,
            SceneryFlags = sceneryFlags,
            ShadowArtId = GetArtIdOrDefault(mob, ObjectField.Shadow),
            UnderlayArtIds = GetIntArrayOrDefault(mob, ObjectField.Underlay),
            OverlayBackArtIds = GetIntArrayOrDefault(mob, ObjectField.OverlayBack),
            OverlayForeArtIds = GetIntArrayOrDefault(mob, ObjectField.OverlayFore),
            IsDead = isDead,
        };
    }

    private static int GetInt32OrDefault(MobData mob, ObjectField field) =>
        mob.GetProperty(field) is { ParseNote: null } property ? property.GetInt32() : 0;

    private static int[] GetIntArrayOrDefault(MobData mob, ObjectField field) =>
        mob.GetProperty(field) is { ParseNote: null } property ? property.GetInt32Array() : [];

    private static ObjectFlags GetObjectFlagsOrDefault(MobData mob) =>
        mob.GetProperty(ObjectField.ObjectFlags) is { ParseNote: null } property
            ? (ObjectFlags)unchecked((uint)property.GetInt32())
            : default;

    private static SceneryFlags GetSceneryFlagsOrDefault(MobData mob) =>
        mob.GetProperty(ObjectField.SceneryFlags) is { ParseNote: null } property
            ? (SceneryFlags)unchecked((uint)property.GetInt32())
            : default;

    private static int GetBlitScaleOrDefault(MobData mob)
    {
        var blitScale = GetInt32OrDefault(mob, ObjectField.BlitScale);
        return blitScale > 0 ? blitScale : 100;
    }

    private static float GetFloatOrDefault(MobData mob, ObjectField field) =>
        mob.GetProperty(field) is { ParseNote: null } property ? property.GetFloat() : 0f;

    private static float GetRotationOrDefault(MobData mob, ArtId currentArtId)
    {
        if (mob.GetProperty(ObjectField.PadIas1) is { ParseNote: null } property)
            return property.GetFloat();

        return GetCritterFamilyRotationFromArtId(currentArtId);
    }

    private static int GetRotationIndex(MobData mob, ArtId currentArtId, float rotation)
    {
        if (mob.GetProperty(ObjectField.PadIas1) is { ParseNote: null })
            return NormalizeRotationIndex(rotation);

        return GetEmbeddedCritterRotationIndex(currentArtId);
    }

    private static float GetCritterFamilyRotationFromArtId(ArtId currentArtId)
    {
        var artType = currentArtId.Value & ArtTypeMask;
        return artType is CritterArtType or MonsterArtType or UniqueNpcArtType
            ? (currentArtId.Value >> ArtIdRotationShift) & 0x7u
            : 0f;
    }

    private static int GetEmbeddedCritterRotationIndex(ArtId currentArtId)
    {
        var artType = currentArtId.Value & ArtTypeMask;
        return artType is CritterArtType or MonsterArtType or UniqueNpcArtType
            ? checked((int)((currentArtId.Value >> ArtIdRotationShift) & 0x7u))
            : 0;
    }

    private static int NormalizeRotationIndex(float rotation)
    {
        if (!float.IsFinite(rotation))
            return 0;

        if (MathF.Abs(rotation) <= 7.5f)
            return NormalizeDiscreteRotationIndex(checked((int)MathF.Round(rotation, MidpointRounding.AwayFromZero)));

        var normalizedTurns = MathF.Abs(rotation) > (MathF.Tau + 0.001f) ? rotation / 360f : rotation / MathF.Tau;
        normalizedTurns -= MathF.Floor(normalizedTurns);
        return NormalizeDiscreteRotationIndex(
            checked((int)MathF.Round(normalizedTurns * 8f, MidpointRounding.AwayFromZero))
        );
    }

    private static int NormalizeDiscreteRotationIndex(int rotationIndex)
    {
        var normalizedRotationIndex = rotationIndex % 8;
        return normalizedRotationIndex < 0 ? normalizedRotationIndex + 8 : normalizedRotationIndex;
    }

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
