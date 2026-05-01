using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// Shared host-neutral tile-space camera and viewport math for map previews.
/// </summary>
public static class EditorMapCameraMath
{
    /// <summary>
    /// Returns the visible tile-space bounds for the supplied viewport and camera state.
    /// </summary>
    public static EditorMapTileBounds GetVisibleTileBounds(
        EditorProjectMapCameraState camera,
        double viewportWidth,
        double viewportHeight,
        double pixelsPerTileAtZoom1
    )
    {
        var pixelsPerTile = GetPixelsPerTile(camera, viewportWidth, viewportHeight, pixelsPerTileAtZoom1);
        var halfVisibleTileWidth = (viewportWidth / pixelsPerTile) / 2d;
        var halfVisibleTileHeight = (viewportHeight / pixelsPerTile) / 2d;

        return new EditorMapTileBounds
        {
            MinTileX = camera.CenterTileX - halfVisibleTileWidth,
            MinTileY = camera.CenterTileY - halfVisibleTileHeight,
            MaxTileX = camera.CenterTileX + halfVisibleTileWidth,
            MaxTileY = camera.CenterTileY + halfVisibleTileHeight,
        };
    }

    /// <summary>
    /// Projects one map-tile X coordinate into viewport pixel space.
    /// </summary>
    public static double ProjectTileX(
        EditorProjectMapCameraState camera,
        double viewportWidth,
        double viewportHeight,
        double pixelsPerTileAtZoom1,
        double tileX
    )
    {
        var pixelsPerTile = GetPixelsPerTile(camera, viewportWidth, viewportHeight, pixelsPerTileAtZoom1);
        return (viewportWidth / 2d) + ((tileX - camera.CenterTileX) * pixelsPerTile);
    }

    /// <summary>
    /// Projects one map-tile Y coordinate into viewport pixel space, treating larger tile Y values as visually higher on screen.
    /// </summary>
    public static double ProjectTileY(
        EditorProjectMapCameraState camera,
        double viewportWidth,
        double viewportHeight,
        double pixelsPerTileAtZoom1,
        double tileY
    )
    {
        var pixelsPerTile = GetPixelsPerTile(camera, viewportWidth, viewportHeight, pixelsPerTileAtZoom1);
        return (viewportHeight / 2d) - ((tileY - camera.CenterTileY) * pixelsPerTile);
    }

    /// <summary>
    /// Converts one viewport pixel X coordinate back into map-tile space.
    /// </summary>
    public static double UnprojectTileX(
        EditorProjectMapCameraState camera,
        double viewportWidth,
        double viewportHeight,
        double pixelsPerTileAtZoom1,
        double viewportX
    )
    {
        var pixelsPerTile = GetPixelsPerTile(camera, viewportWidth, viewportHeight, pixelsPerTileAtZoom1);
        return camera.CenterTileX + ((viewportX - (viewportWidth / 2d)) / pixelsPerTile);
    }

    /// <summary>
    /// Converts one viewport pixel Y coordinate back into map-tile space.
    /// </summary>
    public static double UnprojectTileY(
        EditorProjectMapCameraState camera,
        double viewportWidth,
        double viewportHeight,
        double pixelsPerTileAtZoom1,
        double viewportY
    )
    {
        var pixelsPerTile = GetPixelsPerTile(camera, viewportWidth, viewportHeight, pixelsPerTileAtZoom1);
        return camera.CenterTileY - ((viewportY - (viewportHeight / 2d)) / pixelsPerTile);
    }

    /// <summary>
    /// Hit-tests one scene-preview point against the dense local sector grid and returns the corresponding
    /// sector-local tile plus any preview objects stacked on that tile.
    /// </summary>
    public static EditorMapSceneHit? HitTestScene(
        EditorMapScenePreview scenePreview,
        EditorProjectMapCameraState camera,
        double viewportWidth,
        double viewportHeight,
        double pixelsPerTileAtZoom1,
        double viewportX,
        double viewportY
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ValidateFinite(nameof(viewportX), viewportX);
        ValidateFinite(nameof(viewportY), viewportY);

        if (viewportX < 0d || viewportX >= viewportWidth || viewportY < 0d || viewportY >= viewportHeight)
            return null;

        if (scenePreview.Sectors.Count == 0)
            return null;

        var tileWidth = scenePreview.Sectors[0].TileWidth;
        var tileHeight = scenePreview.Sectors[0].TileHeight;
        if (tileWidth <= 0 || tileHeight <= 0)
            throw new InvalidOperationException("Scene preview sectors must expose positive tile dimensions.");

        var mapTileX = (int)
            Math.Floor(UnprojectTileX(camera, viewportWidth, viewportHeight, pixelsPerTileAtZoom1, viewportX));
        var mapTileY = (int)
            Math.Floor(UnprojectTileY(camera, viewportWidth, viewportHeight, pixelsPerTileAtZoom1, viewportY));

        if (mapTileX < 0 || mapTileY < 0)
            return null;

        return TryCreateSceneHit(scenePreview, mapTileX, mapTileY, out var hit) ? hit : null;
    }

    /// <summary>
    /// Hit-tests one viewport-aligned rectangle against the dense local sector grid and returns one scene hit per
    /// selected map tile inside positioned sectors.
    /// </summary>
    public static IReadOnlyList<EditorMapSceneHit> HitTestSceneArea(
        EditorMapScenePreview scenePreview,
        EditorProjectMapCameraState camera,
        double viewportWidth,
        double viewportHeight,
        double pixelsPerTileAtZoom1,
        double viewportStartX,
        double viewportStartY,
        double viewportEndX,
        double viewportEndY
    )
    {
        if (
            !TryGetSceneAreaTileBounds(
                scenePreview,
                camera,
                viewportWidth,
                viewportHeight,
                pixelsPerTileAtZoom1,
                viewportStartX,
                viewportStartY,
                viewportEndX,
                viewportEndY,
                out var minTileX,
                out var minTileY,
                out var maxTileXExclusive,
                out var maxTileYExclusive
            )
        )
            return [];

        return CollectSceneAreaHits(scenePreview, minTileX, minTileY, maxTileXExclusive, maxTileYExclusive);
    }

    /// <summary>
    /// Hit-tests one viewport-aligned rectangle and returns the corresponding persisted area-selection state when the
    /// rectangle covers one or more positioned sector tiles.
    /// </summary>
    public static EditorProjectMapSelectionState? HitTestSceneAreaSelection(
        EditorMapScenePreview scenePreview,
        EditorProjectMapCameraState camera,
        double viewportWidth,
        double viewportHeight,
        double pixelsPerTileAtZoom1,
        double viewportStartX,
        double viewportStartY,
        double viewportEndX,
        double viewportEndY
    )
    {
        if (
            !TryGetSceneAreaTileBounds(
                scenePreview,
                camera,
                viewportWidth,
                viewportHeight,
                pixelsPerTileAtZoom1,
                viewportStartX,
                viewportStartY,
                viewportEndX,
                viewportEndY,
                out var minTileX,
                out var minTileY,
                out var maxTileXExclusive,
                out var maxTileYExclusive
            )
        )
            return null;

        var hits = CollectSceneAreaHits(scenePreview, minTileX, minTileY, maxTileXExclusive, maxTileYExclusive);
        if (hits.Count == 0)
            return null;

        var primaryHit = hits[0];
        return new EditorProjectMapSelectionState
        {
            SectorAssetPath = primaryHit.SectorAssetPath,
            Tile = primaryHit.Tile,
            ObjectId = primaryHit.ObjectHits.FirstOrDefault()?.ObjectId,
            Area = new EditorProjectMapAreaSelectionState
            {
                MinMapTileX = minTileX,
                MinMapTileY = minTileY,
                MaxMapTileX = maxTileXExclusive - 1,
                MaxMapTileY = maxTileYExclusive - 1,
                ObjectIds = CollectAreaObjectIds(hits),
            },
        };
    }

    /// <summary>
    /// Resolves one persisted area selection back into positioned-sector scene hits, preserving the area's stable tile order.
    /// Tiles that do not currently map to positioned sectors are skipped.
    /// </summary>
    public static IReadOnlyList<EditorMapSceneHit> ResolveSceneAreaSelection(
        EditorMapScenePreview scenePreview,
        EditorProjectMapAreaSelectionState areaSelection
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(areaSelection);

        if (scenePreview.Sectors.Count == 0)
            return [];

        return CollectSceneAreaHits(
            scenePreview,
            areaSelection.MinMapTileX,
            areaSelection.MinMapTileY,
            areaSelection.MaxMapTileX + 1,
            areaSelection.MaxMapTileY + 1
        );
    }

    /// <summary>
    /// Resolves one persisted area selection back into positioned-sector scene-hit groups, preserving stable screen order
    /// both between sectors and within each grouped sector.
    /// </summary>
    public static IReadOnlyList<EditorMapSceneSectorHitGroup> ResolveSceneAreaSelectionBySector(
        EditorMapScenePreview scenePreview,
        EditorProjectMapAreaSelectionState areaSelection
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(areaSelection);

        var hits = ResolveSceneAreaSelection(scenePreview, areaSelection);
        if (hits.Count == 0)
            return [];

        var sectorLookup = scenePreview.Sectors.ToDictionary(
            static sector => sector.AssetPath,
            StringComparer.OrdinalIgnoreCase
        );
        var groupedHits = new List<(EditorMapSectorScenePreview Sector, List<EditorMapSceneHit> Hits)>();
        var groupIndicesBySectorPath = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var hitIndex = 0; hitIndex < hits.Count; hitIndex++)
        {
            var hit = hits[hitIndex];
            if (!groupIndicesBySectorPath.TryGetValue(hit.SectorAssetPath, out var groupIndex))
            {
                if (!sectorLookup.TryGetValue(hit.SectorAssetPath, out var sector))
                    continue;

                groupIndex = groupedHits.Count;
                groupIndicesBySectorPath[hit.SectorAssetPath] = groupIndex;
                groupedHits.Add((sector, [hit]));
                continue;
            }

            groupedHits[groupIndex].Hits.Add(hit);
        }

        var results = new List<EditorMapSceneSectorHitGroup>(groupedHits.Count);
        for (var groupIndex = 0; groupIndex < groupedHits.Count; groupIndex++)
        {
            var (sector, sectorHits) = groupedHits[groupIndex];
            results.Add(
                new EditorMapSceneSectorHitGroup
                {
                    SectorAssetPath = sector.AssetPath,
                    LocalX = sector.LocalX,
                    LocalY = sector.LocalY,
                    Hits = sectorHits,
                }
            );
        }

        return results;
    }

    private static IReadOnlyList<GameObjectGuid> CollectAreaObjectIds(IReadOnlyList<EditorMapSceneHit> hits)
    {
        var objectIds = new List<GameObjectGuid>();

        for (var hitIndex = 0; hitIndex < hits.Count; hitIndex++)
        {
            var objectHits = hits[hitIndex].ObjectHits;
            for (var objectIndex = 0; objectIndex < objectHits.Count; objectIndex++)
            {
                var objectId = objectHits[objectIndex].ObjectId;
                if (!objectIds.Contains(objectId))
                    objectIds.Add(objectId);
            }
        }

        return objectIds;
    }

    private static IReadOnlyList<EditorMapSceneHit> CollectSceneAreaHits(
        EditorMapScenePreview scenePreview,
        int minTileX,
        int minTileY,
        int maxTileXExclusive,
        int maxTileYExclusive
    )
    {
        var hits = new List<EditorMapSceneHit>();
        for (var mapTileY = maxTileYExclusive - 1; mapTileY >= minTileY; mapTileY--)
        {
            for (var mapTileX = minTileX; mapTileX < maxTileXExclusive; mapTileX++)
            {
                if (TryCreateSceneHit(scenePreview, mapTileX, mapTileY, out var hit))
                    hits.Add(hit);
            }
        }

        return hits;
    }

    private static bool TryGetSceneAreaTileBounds(
        EditorMapScenePreview scenePreview,
        EditorProjectMapCameraState camera,
        double viewportWidth,
        double viewportHeight,
        double pixelsPerTileAtZoom1,
        double viewportStartX,
        double viewportStartY,
        double viewportEndX,
        double viewportEndY,
        out int minTileX,
        out int minTileY,
        out int maxTileXExclusive,
        out int maxTileYExclusive
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ValidateFinite(nameof(viewportStartX), viewportStartX);
        ValidateFinite(nameof(viewportStartY), viewportStartY);
        ValidateFinite(nameof(viewportEndX), viewportEndX);
        ValidateFinite(nameof(viewportEndY), viewportEndY);
        ValidateNonNegativeFinite(nameof(viewportWidth), viewportWidth);
        ValidateNonNegativeFinite(nameof(viewportHeight), viewportHeight);

        minTileX = default;
        minTileY = default;
        maxTileXExclusive = default;
        maxTileYExclusive = default;

        if (scenePreview.Sectors.Count == 0 || viewportWidth == 0d || viewportHeight == 0d)
            return false;

        var clampedMinX = Math.Clamp(Math.Min(viewportStartX, viewportEndX), 0d, viewportWidth);
        var clampedMaxX = Math.Clamp(Math.Max(viewportStartX, viewportEndX), 0d, viewportWidth);
        var clampedMinY = Math.Clamp(Math.Min(viewportStartY, viewportEndY), 0d, viewportHeight);
        var clampedMaxY = Math.Clamp(Math.Max(viewportStartY, viewportEndY), 0d, viewportHeight);
        if (clampedMinX == clampedMaxX || clampedMinY == clampedMaxY)
            return false;

        var rawTileX0 = UnprojectTileX(camera, viewportWidth, viewportHeight, pixelsPerTileAtZoom1, clampedMinX);
        var rawTileX1 = UnprojectTileX(camera, viewportWidth, viewportHeight, pixelsPerTileAtZoom1, clampedMaxX);
        var rawTileY0 = UnprojectTileY(camera, viewportWidth, viewportHeight, pixelsPerTileAtZoom1, clampedMinY);
        var rawTileY1 = UnprojectTileY(camera, viewportWidth, viewportHeight, pixelsPerTileAtZoom1, clampedMaxY);

        minTileX = Math.Max(0, (int)Math.Floor(Math.Min(rawTileX0, rawTileX1)));
        minTileY = Math.Max(0, (int)Math.Floor(Math.Min(rawTileY0, rawTileY1)));
        maxTileXExclusive = (int)Math.Ceiling(Math.Max(rawTileX0, rawTileX1));
        maxTileYExclusive = (int)Math.Ceiling(Math.Max(rawTileY0, rawTileY1));

        return maxTileXExclusive > minTileX && maxTileYExclusive > minTileY;
    }

    private static bool TryCreateSceneHit(
        EditorMapScenePreview scenePreview,
        int mapTileX,
        int mapTileY,
        out EditorMapSceneHit hit
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);

        if (scenePreview.Sectors.Count == 0)
        {
            hit = null!;
            return false;
        }

        var tileWidth = scenePreview.Sectors[0].TileWidth;
        var tileHeight = scenePreview.Sectors[0].TileHeight;

        var localSectorX = mapTileX / tileWidth;
        var localSectorY = mapTileY / tileHeight;
        var sector = scenePreview.Sectors.FirstOrDefault(candidate =>
            candidate.LocalX == localSectorX && candidate.LocalY == localSectorY
        );
        if (sector is null)
        {
            hit = null!;
            return false;
        }

        var localTileX = mapTileX - (localSectorX * tileWidth);
        var localTileY = mapTileY - (localSectorY * tileHeight);
        var localTile = new Location(checked((short)localTileX), checked((short)localTileY));

        hit = new EditorMapSceneHit
        {
            MapTileX = mapTileX,
            MapTileY = mapTileY,
            SectorAssetPath = sector.AssetPath,
            Tile = localTile,
            ObjectHits =
            [
                .. sector
                    .Objects.Where(candidate => candidate.Location == localTile)
                    .OrderByDescending(GetObjectSceneDepthBias)
                    .ThenByDescending(GetObjectSceneSpriteHeightBias)
                    .ThenByDescending(candidate => candidate.CollisionHeight),
            ],
        };

        return true;
    }

    private static float GetObjectSceneDepthBias(EditorMapObjectPreview objectPreview) =>
        objectPreview.OffsetY - objectPreview.OffsetZ + GetObjectSceneSpriteCenterYBias(objectPreview);

    private static int GetObjectSceneSpriteCenterYBias(EditorMapObjectPreview objectPreview) =>
        objectPreview.SpriteBounds?.MaxFrameCenterY ?? 0;

    private static int GetObjectSceneSpriteHeightBias(EditorMapObjectPreview objectPreview) =>
        objectPreview.SpriteBounds?.MaxFrameHeight ?? 0;

    /// <summary>
    /// Hit-tests one scene-preview point against the dense local sector grid and returns the corresponding
    /// selection state when the point lands inside a positioned sector.
    /// The returned tile coordinate is local to the selected sector.
    /// </summary>
    public static EditorProjectMapSelectionState? HitTestSceneSelection(
        EditorMapScenePreview scenePreview,
        EditorProjectMapCameraState camera,
        double viewportWidth,
        double viewportHeight,
        double pixelsPerTileAtZoom1,
        double viewportX,
        double viewportY
    )
    {
        var sceneHit = HitTestScene(
            scenePreview,
            camera,
            viewportWidth,
            viewportHeight,
            pixelsPerTileAtZoom1,
            viewportX,
            viewportY
        );
        if (sceneHit is null)
            return null;

        return new EditorProjectMapSelectionState
        {
            SectorAssetPath = sceneHit.SectorAssetPath,
            Tile = sceneHit.Tile,
            ObjectId = sceneHit.ObjectHits.FirstOrDefault()?.ObjectId,
        };
    }

    private static double GetPixelsPerTile(
        EditorProjectMapCameraState camera,
        double viewportWidth,
        double viewportHeight,
        double pixelsPerTileAtZoom1
    )
    {
        ArgumentNullException.ThrowIfNull(camera);
        ValidateNonNegativeFinite(nameof(viewportWidth), viewportWidth);
        ValidateNonNegativeFinite(nameof(viewportHeight), viewportHeight);

        if (!double.IsFinite(pixelsPerTileAtZoom1) || pixelsPerTileAtZoom1 <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pixelsPerTileAtZoom1),
                pixelsPerTileAtZoom1,
                "Pixels-per-tile must be a finite positive value."
            );
        }

        if (!double.IsFinite(camera.Zoom) || camera.Zoom <= 0d)
            throw new ArgumentOutOfRangeException(
                nameof(camera),
                camera.Zoom,
                "Camera zoom must be finite and positive."
            );

        return pixelsPerTileAtZoom1 * camera.Zoom;
    }

    private static void ValidateNonNegativeFinite(string name, double value)
    {
        if (!double.IsFinite(value) || value < 0d)
            throw new ArgumentOutOfRangeException(name, value, "Viewport dimensions must be finite and non-negative.");
    }

    private static void ValidateFinite(string name, double value)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(name, value, "Viewport coordinates must be finite.");
    }
}
