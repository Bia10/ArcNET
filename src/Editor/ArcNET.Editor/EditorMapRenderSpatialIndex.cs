namespace ArcNET.Editor;

internal sealed class EditorMapRenderSpatialIndex
{
    private const double TileBucketSize = 64d;
    private const double ObjectBucketSize = 256d;

    private readonly EditorMapFloorRenderPreview _sceneRender;
    private readonly Dictionary<long, EditorMapFloorTileRenderItem[]> _tilesByMapCoordinate;
    private readonly Dictionary<BucketKey, EditorMapFloorTileRenderItem[]> _tilesByRenderBucket;
    private readonly Dictionary<BucketKey, EditorMapObjectRenderItem[]> _objectsByBucket;

    private EditorMapRenderSpatialIndex(
        EditorMapFloorRenderPreview sceneRender,
        Dictionary<long, EditorMapFloorTileRenderItem[]> tilesByMapCoordinate,
        Dictionary<BucketKey, EditorMapFloorTileRenderItem[]> tilesByRenderBucket,
        Dictionary<BucketKey, EditorMapObjectRenderItem[]> objectsByBucket
    )
    {
        _sceneRender = sceneRender;
        _tilesByMapCoordinate = tilesByMapCoordinate;
        _tilesByRenderBucket = tilesByRenderBucket;
        _objectsByBucket = objectsByBucket;
    }

    public static EditorMapRenderSpatialIndex Build(EditorMapFloorRenderPreview sceneRender)
    {
        ArgumentNullException.ThrowIfNull(sceneRender);

        var tileBuckets = new Dictionary<long, List<EditorMapFloorTileRenderItem>>();
        var tileRenderBuckets = new Dictionary<BucketKey, List<EditorMapFloorTileRenderItem>>();
        AddTiles(
            sceneRender,
            tileBuckets,
            tileRenderBuckets,
            sceneRender.Slices.Count > 0 ? EnumerateSliceTiles(sceneRender) : sceneRender.Tiles
        );

        var objectBuckets = new Dictionary<BucketKey, List<EditorMapObjectRenderItem>>();
        AddObjects(
            objectBuckets,
            sceneRender.Slices.Count > 0 ? EnumerateSliceObjects(sceneRender) : sceneRender.Objects
        );

        return new EditorMapRenderSpatialIndex(
            sceneRender,
            tileBuckets.ToDictionary(
                static pair => pair.Key,
                static pair =>
                {
                    pair.Value.Sort(static (a, b) => b.DrawOrder.CompareTo(a.DrawOrder));
                    return pair.Value.ToArray();
                }
            ),
            tileRenderBuckets.ToDictionary(
                static pair => pair.Key,
                static pair =>
                {
                    pair.Value.Sort(static (a, b) => b.DrawOrder.CompareTo(a.DrawOrder));
                    return pair.Value.ToArray();
                }
            ),
            objectBuckets.ToDictionary(
                static pair => pair.Key,
                static pair =>
                {
                    pair.Value.Sort(static (a, b) => a.DrawOrder.CompareTo(b.DrawOrder));
                    return pair.Value.ToArray();
                }
            )
        );
    }

    public bool TryHitTest(
        double renderX,
        double renderY,
        int mapTileX,
        int mapTileY,
        out EditorMapFloorTileRenderItem? hitTile,
        out IReadOnlyList<EditorMapObjectRenderItem> objectHits
    )
    {
        hitTile = null;
        objectHits = [];

        if (_tilesByMapCoordinate.TryGetValue(CreateTileKey(mapTileX, mapTileY), out var tileCandidates))
            hitTile = FindContainingTile(tileCandidates, renderX, renderY);

        if (
            hitTile is null
            && _tilesByRenderBucket.TryGetValue(CreateBucketKey(renderX, renderY, TileBucketSize), out tileCandidates)
        )
        {
            hitTile = FindContainingTile(tileCandidates, renderX, renderY);
        }

        if (hitTile is null)
            return false;

        if (_objectsByBucket.TryGetValue(CreateBucketKey(renderX, renderY), out var objectCandidates))
        {
            List<EditorMapObjectRenderItem>? hits = null;
            for (var index = 0; index < objectCandidates.Length; index++)
            {
                var obj = objectCandidates[index];
                if (EditorMapSceneRenderSpaceMath.ContainsRenderPoint(obj, renderX, renderY))
                {
                    hits ??= [];
                    hits.Add(obj);
                }
            }

            objectHits = hits?.ToArray() ?? [];
        }

        return true;
    }

    private static void AddTiles(
        EditorMapFloorRenderPreview sceneRender,
        Dictionary<long, List<EditorMapFloorTileRenderItem>> tileBuckets,
        Dictionary<BucketKey, List<EditorMapFloorTileRenderItem>> tileRenderBuckets,
        IEnumerable<EditorMapFloorTileRenderItem> tiles
    )
    {
        foreach (var tile in tiles)
        {
            var key = CreateTileKey(tile.MapTileX, tile.MapTileY);
            if (!tileBuckets.TryGetValue(key, out var bucket))
            {
                bucket = [];
                tileBuckets[key] = bucket;
            }

            bucket.Add(tile);
            AddTileRenderBuckets(sceneRender, tileRenderBuckets, tile);
        }
    }

    private static void AddObjects(
        Dictionary<BucketKey, List<EditorMapObjectRenderItem>> objectBuckets,
        IEnumerable<EditorMapObjectRenderItem> objects
    )
    {
        foreach (var obj in objects)
        {
            var bounds = CreateObjectBounds(obj);
            var minBucketX = GetBucketCoordinate(bounds.Left, ObjectBucketSize);
            var maxBucketX = GetBucketCoordinate(bounds.Right, ObjectBucketSize);
            var minBucketY = GetBucketCoordinate(bounds.Top, ObjectBucketSize);
            var maxBucketY = GetBucketCoordinate(bounds.Bottom, ObjectBucketSize);

            for (var bucketY = minBucketY; bucketY <= maxBucketY; bucketY++)
            {
                for (var bucketX = minBucketX; bucketX <= maxBucketX; bucketX++)
                {
                    var key = new BucketKey(bucketX, bucketY);
                    if (!objectBuckets.TryGetValue(key, out var bucket))
                    {
                        bucket = [];
                        objectBuckets[key] = bucket;
                    }

                    bucket.Add(obj);
                }
            }
        }
    }

    private static IEnumerable<EditorMapFloorTileRenderItem> EnumerateSliceTiles(
        EditorMapFloorRenderPreview sceneRender
    )
    {
        for (var sliceIndex = 0; sliceIndex < sceneRender.Slices.Count; sliceIndex++)
        {
            var sliceTiles = sceneRender.Slices[sliceIndex].Tiles;
            for (var tileIndex = 0; tileIndex < sliceTiles.Count; tileIndex++)
                yield return sliceTiles[tileIndex];
        }
    }

    private static IEnumerable<EditorMapObjectRenderItem> EnumerateSliceObjects(EditorMapFloorRenderPreview sceneRender)
    {
        for (var sliceIndex = 0; sliceIndex < sceneRender.Slices.Count; sliceIndex++)
        {
            var sliceObjects = sceneRender.Slices[sliceIndex].Objects;
            for (var objectIndex = 0; objectIndex < sliceObjects.Count; objectIndex++)
                yield return sliceObjects[objectIndex];
        }
    }

    private static ObjectBounds CreateObjectBounds(EditorMapObjectRenderItem obj)
    {
        var spriteBounds = obj.SpriteBounds;
        if (spriteBounds is null)
            return new ObjectBounds(obj.AnchorX - 8d, obj.AnchorY - 8d, 16d, 16d);

        var (centerX, centerY) = EditorMapFloorRenderBuilder.GetLayoutSpriteCenter(
            obj.ObjectType,
            obj.CurrentArtId,
            spriteBounds
        );
        return new ObjectBounds(
            obj.AnchorX - centerX,
            obj.AnchorY - centerY,
            spriteBounds.MaxFrameWidth,
            spriteBounds.MaxFrameHeight
        );
    }

    private static void AddTileRenderBuckets(
        EditorMapFloorRenderPreview sceneRender,
        Dictionary<BucketKey, List<EditorMapFloorTileRenderItem>> tileRenderBuckets,
        EditorMapFloorTileRenderItem tile
    )
    {
        var halfWidth = Math.Max(1d, sceneRender.TileWidthPixels / 2d);
        var halfHeight = Math.Max(1d, sceneRender.TileHeightPixels / 2d);
        var left = tile.CenterX - halfWidth;
        var right = tile.CenterX + halfWidth;
        var top = tile.CenterY - halfHeight;
        var bottom = tile.CenterY + halfHeight;
        var minBucketX = GetBucketCoordinate(left, TileBucketSize);
        var maxBucketX = GetBucketCoordinate(right, TileBucketSize);
        var minBucketY = GetBucketCoordinate(top, TileBucketSize);
        var maxBucketY = GetBucketCoordinate(bottom, TileBucketSize);

        for (var bucketY = minBucketY; bucketY <= maxBucketY; bucketY++)
        {
            for (var bucketX = minBucketX; bucketX <= maxBucketX; bucketX++)
            {
                var key = new BucketKey(bucketX, bucketY);
                if (!tileRenderBuckets.TryGetValue(key, out var bucket))
                {
                    bucket = [];
                    tileRenderBuckets[key] = bucket;
                }

                bucket.Add(tile);
            }
        }
    }

    private EditorMapFloorTileRenderItem? FindContainingTile(
        EditorMapFloorTileRenderItem[] tileCandidates,
        double renderX,
        double renderY
    )
    {
        for (var index = 0; index < tileCandidates.Length; index++)
        {
            var tile = tileCandidates[index];
            if (EditorMapSceneRenderSpaceMath.ContainsRenderPoint(_sceneRender, tile, renderX, renderY))
                return tile;
        }

        return null;
    }

    private static long CreateTileKey(int mapTileX, int mapTileY) => ((long)mapTileX << 32) ^ (uint)mapTileY;

    private static BucketKey CreateBucketKey(double renderX, double renderY) =>
        CreateBucketKey(renderX, renderY, ObjectBucketSize);

    private static BucketKey CreateBucketKey(double renderX, double renderY, double bucketSize) =>
        new(GetBucketCoordinate(renderX, bucketSize), GetBucketCoordinate(renderY, bucketSize));

    private static int GetBucketCoordinate(double value, double bucketSize) => (int)Math.Floor(value / bucketSize);

    private readonly record struct BucketKey(int X, int Y);

    private readonly record struct ObjectBounds(double Left, double Top, double Width, double Height)
    {
        public double Right => Left + Math.Max(1d, Width);

        public double Bottom => Top + Math.Max(1d, Height);
    }
}
