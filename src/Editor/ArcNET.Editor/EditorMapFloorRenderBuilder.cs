using System.Numerics;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Builds render-ready floor-tile projections from host-neutral scene previews.
/// Sectors are processed in parallel and tile/roof iteration is accelerated with precomputed dense-tile bitmasks.
/// </summary>
public static class EditorMapFloorRenderBuilder
{
    private const int WallTransparencyLeft = 0x0002;
    private const int WallTransparencyRight = 0x0004;
    private const int WallTransDisallow = 0x0001;

    private static readonly bool[,,] RoofCoverageMatrix =
    {
        {
            { false, false, false, false },
            { false, false, false, false },
            { true, true, true, false },
            { true, true, true, false },
        },
        {
            { true, true, true, false },
            { true, true, true, false },
            { true, true, true, false },
            { true, true, true, false },
        },
        {
            { false, false, false, false },
            { false, false, false, false },
            { true, true, true, true },
            { true, true, true, true },
        },
        {
            { true, true, true, false },
            { true, true, true, false },
            { true, true, true, true },
            { true, true, true, true },
        },
        {
            { true, true, true, false },
            { true, true, true, false },
            { true, true, true, false },
            { false, false, false, false },
        },
        {
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, false },
        },
        {
            { false, false, true, true },
            { false, false, true, true },
            { true, true, true, true },
            { true, true, true, true },
        },
        {
            { false, false, false, false },
            { false, false, false, false },
            { false, false, true, true },
            { false, false, true, true },
        },
        {
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
        },
        {
            { false, false, true, true },
            { false, false, true, true },
            { false, false, true, true },
            { false, false, false, false },
        },
        {
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
            { false, false, false, false },
        },
        {
            { false, false, true, true },
            { false, false, true, true },
            { false, false, true, true },
            { false, false, true, true },
        },
        {
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
            { false, false, true, true },
        },
    };

    private sealed record RawTileRenderItem(
        string SectorAssetPath,
        int MapTileX,
        int MapTileY,
        Location Tile,
        ArtId ArtId,
        bool IsBlocked,
        bool HasLight,
        bool HasScript,
        long DrawOrder,
        double CenterX,
        double CenterY
    );

    private sealed record RawTileOverlayRenderItem(
        string SectorAssetPath,
        int MapTileX,
        int MapTileY,
        Location Tile,
        EditorMapTileOverlayKind Kind,
        double SortKey,
        double CenterX,
        double CenterY,
        double SuggestedOpacity,
        uint SuggestedTintColor
    );

    private sealed record RawObjectRenderItem(
        string SectorAssetPath,
        int? SourceObjectIndex,
        GameObjectGuid ObjectId,
        GameObjectGuid ProtoId,
        ObjectType ObjectType,
        ArtId CurrentArtId,
        ObjectFlags Flags,
        int WallFlags,
        SceneryFlags SceneryFlags,
        int MapTileX,
        int MapTileY,
        Location Tile,
        long BaseTileDrawOrder,
        int SameTileOrder,
        double AnchorX,
        double AnchorY,
        EditorMapObjectSpriteBounds? SpriteBounds,
        bool IsTileGridSnapped,
        float Rotation,
        int RotationIndex,
        int BlitScale,
        int BlitFlags,
        uint BlitColor,
        int BlitAlpha,
        bool IsShrunk,
        float RotationPitch,
        bool IsDead,
        bool IsRoofCovered = false,
        bool IsIndoorTile = false,
        int LightFlags = 0,
        ArtId LightAid = default,
        Color? LightColor = null
    );

    private sealed record RawRoofRenderItem(
        string SectorAssetPath,
        Location RoofCell,
        int MapTileX,
        int MapTileY,
        ArtId ArtId,
        long BaseTileDrawOrder,
        double AnchorX,
        double AnchorY
    );

    private sealed record RawAuxiliaryRenderItem(
        string SectorAssetPath,
        GameObjectGuid ParentObjectId,
        ObjectType ParentObjectType,
        EditorMapCommittedRenderLayer CommittedRenderLayer,
        ArtId ArtId,
        EditorMapObjectAuxiliaryRenderLayer Layer,
        int SlotOrder,
        int MapTileX,
        int MapTileY,
        Location Tile,
        long ParentBaseTileDrawOrder,
        int ParentSameTileOrder,
        double AnchorX,
        double AnchorY,
        int RotationIndex,
        int ScalePercent,
        bool IsShrunk,
        bool IsParentDead,
        bool IsRoofCovered,
        uint? SuggestedTintColor = null,
        EditorMapSpriteBlendMode BlendMode = EditorMapSpriteBlendMode.SourceOver
    );

    private sealed class SectorAccumulator
    {
        public readonly List<RawTileRenderItem> RawTiles = [];
        public readonly List<RawTileOverlayRenderItem> RawTileOverlays = [];
        public readonly List<RawObjectRenderItem> RawObjects = [];
        public readonly List<RawRoofRenderItem> RawRoofs = [];
        public readonly List<RawAuxiliaryRenderItem> RawAuxiliaries = [];
        public readonly List<EditorMapLightRenderItem> RawLights = [];
        public string? SectorAssetPath;
        public double MinLeft = double.PositiveInfinity;
        public double MinTop = double.PositiveInfinity;
        public double MaxRight = double.NegativeInfinity;
        public double MaxBottom = double.NegativeInfinity;
        public int MinMapTileX = int.MaxValue;
        public int MinMapTileY = int.MaxValue;
        public int MaxMapTileX = int.MinValue;
        public int MaxMapTileY = int.MinValue;

        public bool HasContent =>
            RawTiles.Count > 0
            || RawTileOverlays.Count > 0
            || RawObjects.Count > 0
            || RawRoofs.Count > 0
            || RawAuxiliaries.Count > 0
            || RawLights.Count > 0;
    }

    private sealed class SectorRenderSliceBuilder(string sectorAssetPath)
    {
        public string SectorAssetPath { get; } = sectorAssetPath;
        public List<EditorMapFloorTileRenderItem> Tiles { get; } = [];
        public List<EditorMapTileOverlayRenderItem> Overlays { get; } = [];
        public List<EditorMapObjectRenderItem> Objects { get; } = [];
        public List<EditorMapObjectAuxiliaryRenderItem> ObjectAuxiliaryItems { get; } = [];
        public List<EditorMapRoofRenderItem> Roofs { get; } = [];
        public List<EditorMapLightRenderItem> Lights { get; } = [];
        public List<EditorMapRenderIndexEntry> Queue { get; } = [];

        public EditorMapSectorRenderSlice Build(
            EditorMapSectorRenderSliceBounds bounds,
            long? revisionOverride = null
        ) =>
            new()
            {
                SectorAssetPath = SectorAssetPath,
                Revision =
                    revisionOverride
                    ?? ComputeSliceRevision(
                        SectorAssetPath,
                        bounds,
                        Queue,
                        Tiles,
                        Overlays,
                        Objects,
                        ObjectAuxiliaryItems,
                        Roofs,
                        Lights
                    ),
                Bounds = bounds,
                Queue = [.. Queue],
                Tiles = [.. Tiles],
                Overlays = [.. Overlays],
                Objects = [.. Objects],
                ObjectAuxiliaryItems = [.. ObjectAuxiliaryItems],
                Roofs = [.. Roofs],
                Lights = [.. Lights],
            };

        public EditorMapSectorRenderSliceBounds CreateFallbackBounds(EditorMapFloorRenderRequest request)
        {
            var minLeft = double.PositiveInfinity;
            var minTop = double.PositiveInfinity;
            var maxRight = double.NegativeInfinity;
            var maxBottom = double.NegativeInfinity;
            var minMapTileX = int.MaxValue;
            var minMapTileY = int.MaxValue;
            var maxMapTileX = int.MinValue;
            var maxMapTileY = int.MinValue;
            var halfTileWidth = request.TileWidthPixels / 2d;
            var halfTileHeight = request.TileHeightPixels / 2d;

            for (var index = 0; index < Tiles.Count; index++)
            {
                var tile = Tiles[index];
                minLeft = Math.Min(minLeft, tile.CenterX - halfTileWidth);
                minTop = Math.Min(minTop, tile.CenterY - halfTileHeight);
                maxRight = Math.Max(maxRight, tile.CenterX + halfTileWidth);
                maxBottom = Math.Max(maxBottom, tile.CenterY + halfTileHeight);
                minMapTileX = Math.Min(minMapTileX, tile.MapTileX);
                minMapTileY = Math.Min(minMapTileY, tile.MapTileY);
                maxMapTileX = Math.Max(maxMapTileX, tile.MapTileX);
                maxMapTileY = Math.Max(maxMapTileY, tile.MapTileY);
            }

            for (var index = 0; index < Overlays.Count; index++)
            {
                var overlay = Overlays[index];
                minLeft = Math.Min(minLeft, overlay.CenterX - halfTileWidth);
                minTop = Math.Min(minTop, overlay.CenterY - halfTileHeight);
                maxRight = Math.Max(maxRight, overlay.CenterX + halfTileWidth);
                maxBottom = Math.Max(maxBottom, overlay.CenterY + halfTileHeight);
                minMapTileX = Math.Min(minMapTileX, overlay.MapTileX);
                minMapTileY = Math.Min(minMapTileY, overlay.MapTileY);
                maxMapTileX = Math.Max(maxMapTileX, overlay.MapTileX);
                maxMapTileY = Math.Max(maxMapTileY, overlay.MapTileY);
            }

            for (var index = 0; index < Objects.Count; index++)
            {
                var obj = Objects[index];
                minLeft = Math.Min(minLeft, obj.AnchorX);
                minTop = Math.Min(minTop, obj.AnchorY);
                maxRight = Math.Max(maxRight, obj.AnchorX);
                maxBottom = Math.Max(maxBottom, obj.AnchorY);
                minMapTileX = Math.Min(minMapTileX, obj.MapTileX);
                minMapTileY = Math.Min(minMapTileY, obj.MapTileY);
                maxMapTileX = Math.Max(maxMapTileX, obj.MapTileX);
                maxMapTileY = Math.Max(maxMapTileY, obj.MapTileY);
            }

            for (var index = 0; index < ObjectAuxiliaryItems.Count; index++)
            {
                var auxiliary = ObjectAuxiliaryItems[index];
                minLeft = Math.Min(minLeft, auxiliary.AnchorX);
                minTop = Math.Min(minTop, auxiliary.AnchorY);
                maxRight = Math.Max(maxRight, auxiliary.AnchorX);
                maxBottom = Math.Max(maxBottom, auxiliary.AnchorY);
                minMapTileX = Math.Min(minMapTileX, auxiliary.MapTileX);
                minMapTileY = Math.Min(minMapTileY, auxiliary.MapTileY);
                maxMapTileX = Math.Max(maxMapTileX, auxiliary.MapTileX);
                maxMapTileY = Math.Max(maxMapTileY, auxiliary.MapTileY);
            }

            for (var index = 0; index < Roofs.Count; index++)
            {
                var roof = Roofs[index];
                ExpandRoofBounds(
                    request.ViewMode,
                    request.TileWidthPixels,
                    request.TileHeightPixels,
                    roof.AnchorX,
                    roof.AnchorY,
                    ref minLeft,
                    ref minTop,
                    ref maxRight,
                    ref maxBottom
                );
                minMapTileX = Math.Min(minMapTileX, roof.MapTileX);
                minMapTileY = Math.Min(minMapTileY, roof.MapTileY);
                maxMapTileX = Math.Max(maxMapTileX, roof.MapTileX + roof.FootprintTileWidth - 1);
                maxMapTileY = Math.Max(maxMapTileY, roof.MapTileY + roof.FootprintTileHeight - 1);
            }

            for (var index = 0; index < Lights.Count; index++)
            {
                var light = Lights[index];
                minLeft = Math.Min(minLeft, light.AnchorX);
                minTop = Math.Min(minTop, light.AnchorY);
                maxRight = Math.Max(maxRight, light.AnchorX);
                maxBottom = Math.Max(maxBottom, light.AnchorY);
                minMapTileX = Math.Min(minMapTileX, light.MapTileX);
                minMapTileY = Math.Min(minMapTileY, light.MapTileY);
                maxMapTileX = Math.Max(maxMapTileX, light.MapTileX);
                maxMapTileY = Math.Max(maxMapTileY, light.MapTileY);
            }

            if (double.IsInfinity(minLeft) || minMapTileX == int.MaxValue)
                return new EditorMapSectorRenderSliceBounds(0d, 0d, 0d, 0d, 0, 0, 0, 0);

            return new EditorMapSectorRenderSliceBounds(
                Left: minLeft,
                Top: minTop,
                Width: Math.Max(0d, maxRight - minLeft),
                Height: Math.Max(0d, maxBottom - minTop),
                MinMapTileX: minMapTileX,
                MinMapTileY: minMapTileY,
                MaxMapTileX: maxMapTileX,
                MaxMapTileY: maxMapTileY
            );
        }
    }

    private static long ComputeSliceRevision(
        string sectorAssetPath,
        EditorMapSectorRenderSliceBounds bounds,
        IReadOnlyList<EditorMapRenderIndexEntry> queue,
        IReadOnlyList<EditorMapFloorTileRenderItem> tiles,
        IReadOnlyList<EditorMapTileOverlayRenderItem> overlays,
        IReadOnlyList<EditorMapObjectRenderItem> objects,
        IReadOnlyList<EditorMapObjectAuxiliaryRenderItem> objectAuxiliaryItems,
        IReadOnlyList<EditorMapRoofRenderItem> roofs,
        IReadOnlyList<EditorMapLightRenderItem> lights
    )
    {
        var hash = new StableRevisionHash();
        hash.Add(sectorAssetPath);
        hash.Add(bounds.Left);
        hash.Add(bounds.Top);
        hash.Add(bounds.Width);
        hash.Add(bounds.Height);
        hash.Add(bounds.MinMapTileX);
        hash.Add(bounds.MinMapTileY);
        hash.Add(bounds.MaxMapTileX);
        hash.Add(bounds.MaxMapTileY);

        hash.Add(queue.Count);
        for (var index = 0; index < queue.Count; index++)
        {
            var entry = queue[index];
            hash.Add((int)entry.Kind);
            hash.Add(entry.PayloadIndex);
            hash.Add(entry.SortKey);
            hash.Add(entry.DrawOrder);
        }

        hash.Add(tiles.Count);
        for (var index = 0; index < tiles.Count; index++)
            AddTileRevision(ref hash, tiles[index]);

        hash.Add(overlays.Count);
        for (var index = 0; index < overlays.Count; index++)
            AddOverlayRevision(ref hash, overlays[index]);

        hash.Add(objects.Count);
        for (var index = 0; index < objects.Count; index++)
            AddObjectRevision(ref hash, objects[index]);

        hash.Add(objectAuxiliaryItems.Count);
        for (var index = 0; index < objectAuxiliaryItems.Count; index++)
            AddAuxiliaryRevision(ref hash, objectAuxiliaryItems[index]);

        hash.Add(roofs.Count);
        for (var index = 0; index < roofs.Count; index++)
            AddRoofRevision(ref hash, roofs[index]);

        hash.Add(lights.Count);
        for (var index = 0; index < lights.Count; index++)
            AddLightRevision(ref hash, lights[index]);

        return hash.ToInt64();
    }

    private static long ComputeSceneRevision(
        string mapName,
        EditorMapFloorRenderRequest request,
        IReadOnlyList<EditorMapSectorRenderSlice> slices,
        double widthPixels,
        double heightPixels
    )
    {
        var hash = new StableRevisionHash();
        hash.Add(mapName);
        hash.Add((int)request.ViewMode);
        hash.Add(request.TileWidthPixels);
        hash.Add(request.TileHeightPixels);
        hash.Add(widthPixels);
        hash.Add(heightPixels);
        hash.Add(request.IncludeEditorObjectStateTint);
        hash.Add(request.IncludeFloorLightTint);
        hash.Add(request.AmbientLighting?.ToString());
        hash.Add(slices.Count);
        for (var index = 0; index < slices.Count; index++)
        {
            var slice = slices[index];
            hash.Add(slice.SectorAssetPath);
            hash.Add(slice.Bounds.Left);
            hash.Add(slice.Bounds.Top);
            hash.Add(slice.Bounds.Width);
            hash.Add(slice.Bounds.Height);
            hash.Add(slice.Bounds.MinMapTileX);
            hash.Add(slice.Bounds.MinMapTileY);
            hash.Add(slice.Bounds.MaxMapTileX);
            hash.Add(slice.Bounds.MaxMapTileY);
        }

        return hash.ToInt64();
    }

    private static void AddTileRevision(ref StableRevisionHash hash, EditorMapFloorTileRenderItem tile)
    {
        hash.Add(tile.SectorAssetPath);
        hash.Add(tile.MapTileX);
        hash.Add(tile.MapTileY);
        hash.Add(tile.Tile.X);
        hash.Add(tile.Tile.Y);
        hash.Add(tile.ArtId.Value);
        hash.Add(tile.IsBlocked);
        hash.Add(tile.HasLight);
        hash.Add(tile.HasScript);
        hash.Add(tile.DrawOrder);
        hash.Add(tile.CenterX);
        hash.Add(tile.CenterY);
        hash.Add(tile.SuggestedTintColor);
        if (tile.LightDiagnostics is { } diagnostics)
        {
            hash.Add(true);
            hash.Add(diagnostics.TopLeft);
            hash.Add(diagnostics.TopCenter);
            hash.Add(diagnostics.TopRight);
            hash.Add(diagnostics.MiddleLeft);
            hash.Add(diagnostics.MiddleCenter);
            hash.Add(diagnostics.MiddleRight);
            hash.Add(diagnostics.BottomLeft);
            hash.Add(diagnostics.BottomCenter);
            hash.Add(diagnostics.BottomRight);
        }
        else
        {
            hash.Add(false);
        }
    }

    private static void AddOverlayRevision(ref StableRevisionHash hash, EditorMapTileOverlayRenderItem overlay)
    {
        hash.Add(overlay.SectorAssetPath);
        hash.Add(overlay.MapTileX);
        hash.Add(overlay.MapTileY);
        hash.Add(overlay.Tile.X);
        hash.Add(overlay.Tile.Y);
        hash.Add((int)overlay.Kind);
        hash.Add(overlay.DrawOrder);
        hash.Add(overlay.CenterX);
        hash.Add(overlay.CenterY);
        hash.Add(overlay.SuggestedOpacity);
        hash.Add(overlay.SuggestedTintColor);
    }

    private static void AddObjectRevision(ref StableRevisionHash hash, EditorMapObjectRenderItem obj)
    {
        hash.Add(obj.SectorAssetPath);
        hash.Add(obj.SourceObjectIndex ?? -1);
        hash.Add(obj.ObjectId.ToString());
        hash.Add(obj.ProtoId.ToString());
        hash.Add((int)obj.ObjectType);
        hash.Add(obj.CommittedRenderLayer.HasValue);
        hash.Add(obj.CommittedRenderLayer is { } committedLayer ? (int)committedLayer : -1);
        hash.Add(obj.CurrentArtId.Value);
        hash.Add(obj.Flags.ToString());
        hash.Add(obj.WallFlags);
        hash.Add(obj.SceneryFlags.ToString());
        hash.Add(obj.MapTileX);
        hash.Add(obj.MapTileY);
        hash.Add(obj.Tile.X);
        hash.Add(obj.Tile.Y);
        hash.Add(obj.DrawOrder);
        hash.Add(obj.SameTileOrder);
        hash.Add(obj.IsDead);
        hash.Add(obj.AnchorX);
        hash.Add(obj.AnchorY);
        AddSpriteBoundsRevision(ref hash, obj.SpriteBounds);
        hash.Add(obj.IsTileGridSnapped);
        hash.Add(obj.Rotation);
        hash.Add(obj.RotationIndex);
        hash.Add(obj.BlitScale);
        hash.Add(obj.BlitFlags);
        hash.Add(obj.BlitColor);
        hash.Add(obj.BlitAlpha);
        hash.Add(obj.IsShrunk);
        hash.Add(obj.RotationPitch);
        hash.Add(obj.IsRoofCovered);
        hash.Add(obj.IsIndoorTile);
        hash.Add(obj.LightFlags);
        hash.Add(obj.LightAid.Value);
        hash.Add(obj.LightColor?.ToString());
    }

    private static void AddAuxiliaryRevision(ref StableRevisionHash hash, EditorMapObjectAuxiliaryRenderItem auxiliary)
    {
        hash.Add(auxiliary.SectorAssetPath);
        hash.Add(auxiliary.ParentObjectId.ToString());
        hash.Add((int)auxiliary.ParentObjectType);
        hash.Add((int)auxiliary.CommittedRenderLayer);
        hash.Add(auxiliary.ArtId.Value);
        hash.Add((int)auxiliary.Layer);
        hash.Add(auxiliary.MapTileX);
        hash.Add(auxiliary.MapTileY);
        hash.Add(auxiliary.Tile.X);
        hash.Add(auxiliary.Tile.Y);
        hash.Add(auxiliary.DrawOrder);
        hash.Add(auxiliary.AnchorX);
        hash.Add(auxiliary.AnchorY);
        hash.Add(auxiliary.UseLightMaskTint);
        hash.Add(auxiliary.SuggestedTintColor);
        hash.Add(auxiliary.RotationIndex);
        hash.Add(auxiliary.ScalePercent);
        hash.Add(auxiliary.IsShrunk);
        hash.Add((int)auxiliary.BlendMode);
        hash.Add(auxiliary.IsRoofCovered);
    }

    private static void AddRoofRevision(ref StableRevisionHash hash, EditorMapRoofRenderItem roof)
    {
        hash.Add(roof.SectorAssetPath);
        hash.Add(roof.RoofCell.X);
        hash.Add(roof.RoofCell.Y);
        hash.Add(roof.MapTileX);
        hash.Add(roof.MapTileY);
        hash.Add(roof.ArtId.Value);
        hash.Add(roof.DrawOrder);
        hash.Add(roof.AnchorX);
        hash.Add(roof.AnchorY);
    }

    private static void AddLightRevision(ref StableRevisionHash hash, EditorMapLightRenderItem light)
    {
        hash.Add(light.SectorAssetPath);
        hash.Add(light.MapTileX);
        hash.Add(light.MapTileY);
        hash.Add(light.Tile.X);
        hash.Add(light.Tile.Y);
        hash.Add(light.ArtId.Value);
        hash.Add(light.DrawOrder);
        hash.Add(light.AnchorX);
        hash.Add(light.AnchorY);
        hash.Add(light.SuggestedTintColor);
        hash.Add(light.SuggestedOpacity);
        hash.Add((int)light.Flags);
    }

    private static void AddSpriteBoundsRevision(ref StableRevisionHash hash, EditorMapObjectSpriteBounds? bounds)
    {
        if (bounds is null)
        {
            hash.Add(false);
            return;
        }

        hash.Add(true);
        hash.Add(bounds.MaxFrameWidth);
        hash.Add(bounds.MaxFrameHeight);
        hash.Add(bounds.MaxFrameCenterX);
        hash.Add(bounds.MaxFrameCenterY);
    }

    private struct StableRevisionHash
    {
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;
        private ulong _value = OffsetBasis;

        public StableRevisionHash() { }

        public void Add(bool value) => Add(value ? 1 : 0);

        public void Add(int value) => Add((long)value);

        public void Add(short value) => Add((int)value);

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

        public void Add(float value) => Add(BitConverter.SingleToInt32Bits(value));

        public void Add(double value) => Add(BitConverter.DoubleToInt64Bits(value));

        public void Add(uint? value)
        {
            Add(value.HasValue);
            if (value is { } resolvedValue)
                Add(resolvedValue);
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

    private sealed class SceneSectorLookup(
        IReadOnlyList<EditorMapSectorScenePreview> sectors,
        int sectorTileWidth,
        int sectorTileHeight
    )
    {
        private readonly Dictionary<(int LocalX, int LocalY), EditorMapSectorScenePreview> _sectorsByLocalCoordinate =
            sectors.ToDictionary(static sector => (sector.LocalX, sector.LocalY));

        public bool TryGetSectorTile(
            int mapTileX,
            int mapTileY,
            out EditorMapSectorScenePreview sector,
            out int localTileX,
            out int localTileY
        )
        {
            var localX = FloorDivide(mapTileX, sectorTileWidth);
            var localY = FloorDivide(mapTileY, sectorTileHeight);
            localTileX = PositiveModulo(mapTileX, sectorTileWidth);
            localTileY = PositiveModulo(mapTileY, sectorTileHeight);

            if (_sectorsByLocalCoordinate.TryGetValue((localX, localY), out sector!))
                return true;

            sector = null!;
            return false;
        }
    }

    /// <summary>
    /// Builds one render-ready floor preview from <paramref name="scenePreview"/>.
    /// Sectors are processed in parallel; tile and roof iteration is accelerated with precomputed dense-tile bitmasks.
    /// </summary>
    public static EditorMapFloorRenderPreview Build(
        EditorMapScenePreview scenePreview,
        EditorMapFloorRenderRequest? request = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);

        request ??= new EditorMapFloorRenderRequest();
        ValidateRequest(request);

        if (scenePreview.Sectors.Count == 0)
            return CreateEmptyPreview(scenePreview.MapName, request);

        var sectorTileWidth = scenePreview.Sectors[0].TileWidth;
        var sectorTileHeight = scenePreview.Sectors[0].TileHeight;
        if (sectorTileWidth <= 0 || sectorTileHeight <= 0)
            throw new InvalidOperationException("Scene preview sectors must expose positive tile dimensions.");

        var sectors = scenePreview
            .Sectors.OrderBy(static sector => sector.LocalY)
            .ThenBy(static sector => sector.LocalX)
            .ToArray();
        var materializedTerrainSectorPaths = request.MaterializedTerrainSectorAssetPaths;
        var ambientLightingBySectorAssetPath = BuildSectorAmbientLightingLookup(sectors, request.AmbientLighting);
        var mapTileWidth = checked(scenePreview.Width * sectorTileWidth);
        var sceneSectorLookup = new SceneSectorLookup(sectors, sectorTileWidth, sectorTileHeight);
        var materializedTerrainSectorCount = 0;

        // Phase 1: Collect one owned raw accumulator per sector to avoid retaining worker-local bags.
        var accumulators = new SectorAccumulator[sectors.Length];
        Parallel.For(
            0,
            sectors.Length,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = EditorParallelism.InteractiveMaxDegreeOfParallelism,
            },
            sectorIndex =>
            {
                var materializeTerrain = ShouldMaterializeTerrain(sectors[sectorIndex], materializedTerrainSectorPaths);
                if (materializeTerrain)
                    System.Threading.Interlocked.Increment(ref materializedTerrainSectorCount);

                var local = new SectorAccumulator { SectorAssetPath = sectors[sectorIndex].AssetPath };
                ProcessSector(
                    sectors[sectorIndex],
                    request,
                    sectorTileWidth,
                    sectorTileHeight,
                    mapTileWidth,
                    sceneSectorLookup,
                    materializeTerrain,
                    local
                );
                accumulators[sectorIndex] = local;
            }
        );

        // Phase 1b: Merge accumulators — pre-count to avoid reallocations.
        var totalTileCount = 0;
        var totalOverlayCount = 0;
        var totalObjectCount = 0;
        var totalRoofCount = 0;
        var totalAuxiliaryCount = 0;
        var totalLightCount = 0;
        for (var accumulatorIndex = 0; accumulatorIndex < accumulators.Length; accumulatorIndex++)
        {
            var acc = accumulators[accumulatorIndex];
            if (acc is null)
                continue;

            totalTileCount += acc.RawTiles.Count;
            totalOverlayCount += acc.RawTileOverlays.Count;
            totalObjectCount += acc.RawObjects.Count;
            totalRoofCount += acc.RawRoofs.Count;
            totalAuxiliaryCount += acc.RawAuxiliaries.Count;
            totalLightCount += acc.RawLights.Count;
        }

        var rawTiles = new List<RawTileRenderItem>(totalTileCount);
        var rawTileOverlays = new List<RawTileOverlayRenderItem>(totalOverlayCount);
        var rawObjects = new List<RawObjectRenderItem>(totalObjectCount);
        var rawRoofs = new List<RawRoofRenderItem>(totalRoofCount);
        var rawAuxiliaries = new List<RawAuxiliaryRenderItem>(totalAuxiliaryCount);
        var rawLights = new List<EditorMapLightRenderItem>(totalLightCount);
        var minLeft = double.PositiveInfinity;
        var minTop = double.PositiveInfinity;
        var maxRight = double.NegativeInfinity;
        var maxBottom = double.NegativeInfinity;

        var sectorBoundsByAssetPath = new Dictionary<string, EditorMapSectorRenderSliceBounds>(
            StringComparer.OrdinalIgnoreCase
        );
        for (var accumulatorIndex = 0; accumulatorIndex < accumulators.Length; accumulatorIndex++)
        {
            var local = accumulators[accumulatorIndex];
            if (local is null)
                continue;

            rawTiles.AddRange(local.RawTiles);
            rawTileOverlays.AddRange(local.RawTileOverlays);
            rawObjects.AddRange(local.RawObjects);
            rawRoofs.AddRange(local.RawRoofs);
            rawAuxiliaries.AddRange(local.RawAuxiliaries);
            rawLights.AddRange(local.RawLights);

            if (local.HasContent && !string.IsNullOrWhiteSpace(local.SectorAssetPath))
                sectorBoundsByAssetPath[local.SectorAssetPath] = CreateSectorBounds(local);

            if (local.MinLeft < minLeft)
                minLeft = local.MinLeft;
            if (local.MinTop < minTop)
                minTop = local.MinTop;
            if (local.MaxRight > maxRight)
                maxRight = local.MaxRight;
            if (local.MaxBottom > maxBottom)
                maxBottom = local.MaxBottom;
        }

        if (rawTiles.Count == 0)
            return CreateEmptyPreview(scenePreview.MapName, request);

        // Phase 2: Sort and build final output.
        cancellationToken.ThrowIfCancellationRequested();
        ApplySceneBoundsOverride(request, ref minLeft, ref minTop, ref maxRight, ref maxBottom);
        var offsetX = -minLeft;
        var offsetY = -minTop;

        SortRawItems(rawTiles, rawTileOverlays, rawObjects, rawRoofs, rawAuxiliaries);
        cancellationToken.ThrowIfCancellationRequested();

        return BuildResult(
            scenePreview.MapName,
            request,
            sectors,
            rawTiles,
            rawTileOverlays,
            rawObjects,
            rawRoofs,
            rawAuxiliaries,
            rawLights,
            ambientLightingBySectorAssetPath,
            sectorBoundsByAssetPath,
            offsetX,
            offsetY,
            minLeft,
            maxRight,
            minTop,
            maxBottom,
            materializedTerrainSectorCount,
            sectors.Length
        );
    }

    /// <summary>
    /// Builds a delta floor render preview by replacing one sector's items in <paramref name="existingPreview"/>.
    /// Only the changed sector is re-processed; all other sector entries are preserved and re-sorted alongside the new items.
    /// This is significantly cheaper than a full rebuild when editing one sector at a time.
    /// </summary>
    public static EditorMapFloorRenderPreview BuildDelta(
        EditorMapFloorRenderPreview existingPreview,
        EditorMapScenePreview scenePreview,
        string changedSectorAssetPath,
        EditorMapFloorRenderRequest? request = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(existingPreview);
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentException.ThrowIfNullOrWhiteSpace(changedSectorAssetPath);

        if (scenePreview.Sectors.Count == 0)
        {
            request ??= CreateDeltaFallbackRequest(existingPreview, scenePreview);
            ValidateRequest(request);
            return CreateEmptyPreview(scenePreview.MapName, request);
        }

        request ??= CreateDeltaFallbackRequest(existingPreview, scenePreview);
        ValidateRequest(request);

        if (!CanApplyDelta(existingPreview, request) || request.IncludeFloorLightTint)
            return Build(scenePreview, request, cancellationToken);

        var sectorTileWidth = scenePreview.Sectors[0].TileWidth;
        var sectorTileHeight = scenePreview.Sectors[0].TileHeight;
        if (sectorTileWidth <= 0 || sectorTileHeight <= 0)
            throw new InvalidOperationException("Scene preview sectors must expose positive tile dimensions.");

        var sectors = scenePreview
            .Sectors.OrderBy(static sector => sector.LocalY)
            .ThenBy(static sector => sector.LocalX)
            .ToArray();
        var ambientLightingBySectorAssetPath = BuildSectorAmbientLightingLookup(sectors, request.AmbientLighting);
        var affectedSectorAssetPaths = GetAffectedSectorAssetPaths(sectors, changedSectorAssetPath);
        if (affectedSectorAssetPaths.Count == 0)
            return Build(scenePreview, request, cancellationToken);

        var mapTileWidth = checked(scenePreview.Width * sectorTileWidth);
        var sceneSectorLookup = new SceneSectorLookup(sectors, sectorTileWidth, sectorTileHeight);
        var objectSourceLookup = BuildObjectSourceLookup(sectors, affectedSectorAssetPaths);

        var rawTiles = new List<RawTileRenderItem>(existingPreview.Tiles.Count);
        var rawTileOverlays = new List<RawTileOverlayRenderItem>(existingPreview.Overlays.Count);
        var rawObjects = new List<RawObjectRenderItem>(existingPreview.Objects.Count);
        var rawRoofs = new List<RawRoofRenderItem>(existingPreview.Roofs.Count);
        var rawAuxiliaries = new List<RawAuxiliaryRenderItem>(existingPreview.ObjectAuxiliaryItems.Count);
        var rawLights = new List<EditorMapLightRenderItem>(existingPreview.Lights.Count);
        var minLeft = double.PositiveInfinity;
        var minTop = double.PositiveInfinity;
        var maxRight = double.NegativeInfinity;
        var maxBottom = double.NegativeInfinity;
        var sectorBoundsByAssetPath = new Dictionary<string, EditorMapSectorRenderSliceBounds>(
            StringComparer.OrdinalIgnoreCase
        );
        var preservedSliceRevisionsByAssetPath = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        for (var sliceIndex = 0; sliceIndex < existingPreview.Slices.Count; sliceIndex++)
        {
            var slice = existingPreview.Slices[sliceIndex];
            if (affectedSectorAssetPaths.Contains(slice.SectorAssetPath))
                continue;

            sectorBoundsByAssetPath[slice.SectorAssetPath] = slice.Bounds;
            preservedSliceRevisionsByAssetPath[slice.SectorAssetPath] = slice.Revision;
        }

        var retainedTiles = RemoveSectorItems(existingPreview.Tiles, affectedSectorAssetPaths);
        for (var index = 0; index < retainedTiles.Count; index++)
        {
            var tile = retainedTiles[index];
            rawTiles.Add(RehydrateRawTile(tile, existingPreview, request, mapTileWidth));
            ExpandRetainedTileBounds(
                tile,
                existingPreview,
                request,
                ref minLeft,
                ref minTop,
                ref maxRight,
                ref maxBottom
            );
        }

        var retainedOverlays = RemoveSectorItems(existingPreview.Overlays, affectedSectorAssetPaths);
        for (var index = 0; index < retainedOverlays.Count; index++)
            rawTileOverlays.Add(
                RehydrateRawTileOverlay(retainedOverlays[index], existingPreview, request, mapTileWidth)
            );

        var retainedObjects = RemoveSectorItems(existingPreview.Objects, affectedSectorAssetPaths);
        for (var index = 0; index < retainedObjects.Count; index++)
        {
            var obj = retainedObjects[index];
            rawObjects.Add(RehydrateRawObject(obj, existingPreview, request, mapTileWidth, objectSourceLookup));
            ExpandRetainedObjectBounds(
                obj,
                existingPreview,
                request,
                ref minLeft,
                ref minTop,
                ref maxRight,
                ref maxBottom
            );
        }

        var retainedRoofs = RemoveSectorItems(existingPreview.Roofs, affectedSectorAssetPaths);
        for (var index = 0; index < retainedRoofs.Count; index++)
        {
            var roof = retainedRoofs[index];
            rawRoofs.Add(RehydrateRawRoof(roof, existingPreview, request, mapTileWidth));
            ExpandRetainedRoofBounds(
                roof,
                existingPreview,
                request,
                ref minLeft,
                ref minTop,
                ref maxRight,
                ref maxBottom
            );
        }

        var auxiliarySlotOrdinals =
            new Dictionary<(GameObjectGuid ParentObjectId, EditorMapObjectAuxiliaryRenderLayer Layer), int>();
        var retainedAuxiliaries = RemoveSectorItems(existingPreview.ObjectAuxiliaryItems, affectedSectorAssetPaths);
        for (var index = 0; index < retainedAuxiliaries.Count; index++)
        {
            rawAuxiliaries.Add(
                RehydrateRawAuxiliary(
                    retainedAuxiliaries[index],
                    existingPreview,
                    request,
                    mapTileWidth,
                    objectSourceLookup,
                    auxiliarySlotOrdinals
                )
            );
        }

        var retainedLights = RemoveSectorItems(existingPreview.Lights, affectedSectorAssetPaths);
        for (var index = 0; index < retainedLights.Count; index++)
            rawLights.Add(RehydrateRawLight(retainedLights[index], existingPreview));

        for (var index = 0; index < sectors.Length; index++)
        {
            var sector = sectors[index];
            if (!affectedSectorAssetPaths.Contains(sector.AssetPath))
                continue;

            cancellationToken.ThrowIfCancellationRequested();
            var local = new SectorAccumulator();
            ProcessSector(
                sector,
                request,
                sectorTileWidth,
                sectorTileHeight,
                mapTileWidth,
                sceneSectorLookup,
                materializeTerrain: true,
                local
            );

            rawTiles.AddRange(local.RawTiles);
            rawTileOverlays.AddRange(local.RawTileOverlays);
            rawObjects.AddRange(local.RawObjects);
            rawRoofs.AddRange(local.RawRoofs);
            rawAuxiliaries.AddRange(local.RawAuxiliaries);
            rawLights.AddRange(local.RawLights);

            if (local.HasContent && !string.IsNullOrWhiteSpace(local.SectorAssetPath))
                sectorBoundsByAssetPath[local.SectorAssetPath] = CreateSectorBounds(local);

            if (local.MinLeft < minLeft)
                minLeft = local.MinLeft;
            if (local.MinTop < minTop)
                minTop = local.MinTop;
            if (local.MaxRight > maxRight)
                maxRight = local.MaxRight;
            if (local.MaxBottom > maxBottom)
                maxBottom = local.MaxBottom;
        }

        if (rawTiles.Count == 0)
            return CreateEmptyPreview(scenePreview.MapName, request);

        ApplySceneBoundsOverride(request, ref minLeft, ref minTop, ref maxRight, ref maxBottom);
        var offsetX = -minLeft;
        var offsetY = -minTop;
        SortRawItems(rawTiles, rawTileOverlays, rawObjects, rawRoofs, rawAuxiliaries);

        return BuildResult(
            scenePreview.MapName,
            request,
            sectors,
            rawTiles,
            rawTileOverlays,
            rawObjects,
            rawRoofs,
            rawAuxiliaries,
            rawLights,
            ambientLightingBySectorAssetPath,
            sectorBoundsByAssetPath,
            offsetX,
            offsetY,
            minLeft,
            maxRight,
            minTop,
            maxBottom,
            scenePreview.Sectors.Count,
            scenePreview.Sectors.Count,
            preservedSliceRevisionsByAssetPath
        );
    }

    private static List<T> RemoveSectorItems<T>(IReadOnlyList<T> items, string sectorAssetPath)
    {
        var sectorAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { sectorAssetPath };
        return RemoveSectorItems(items, sectorAssetPaths);
    }

    private static List<T> RemoveSectorItems<T>(IReadOnlyList<T> items, IReadOnlySet<string> sectorAssetPaths)
    {
        var result = new List<T>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (!ItemBelongsToSector(item, sectorAssetPaths))
                result.Add(item);
        }

        return result;
    }

    internal static EditorMapFloorRenderBounds? TryCreateTerrainBounds(
        EditorMapScenePreview scenePreview,
        EditorMapFloorRenderRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(request);

        if (scenePreview.Sectors.Count == 0)
            return null;

        var sectorTileWidth = scenePreview.Sectors[0].TileWidth;
        var sectorTileHeight = scenePreview.Sectors[0].TileHeight;
        if (sectorTileWidth <= 0 || sectorTileHeight <= 0)
            return null;

        var halfTileWidth = request.TileWidthPixels / 2d;
        var halfTileHeight = request.TileHeightPixels / 2d;
        var minLeft = double.PositiveInfinity;
        var minTop = double.PositiveInfinity;
        var maxRight = double.NegativeInfinity;
        var maxBottom = double.NegativeInfinity;

        for (var sectorIndex = 0; sectorIndex < scenePreview.Sectors.Count; sectorIndex++)
        {
            var sector = scenePreview.Sectors[sectorIndex];
            var minTileX = sector.LocalX * sectorTileWidth;
            var minTileY = sector.LocalY * sectorTileHeight;
            var maxTileX = minTileX + sectorTileWidth - 1;
            var maxTileY = minTileY + sectorTileHeight - 1;

            ExpandTerrainBoundsForTile(
                request,
                minTileX,
                minTileY,
                halfTileWidth,
                halfTileHeight,
                ref minLeft,
                ref minTop,
                ref maxRight,
                ref maxBottom
            );
            ExpandTerrainBoundsForTile(
                request,
                minTileX,
                maxTileY,
                halfTileWidth,
                halfTileHeight,
                ref minLeft,
                ref minTop,
                ref maxRight,
                ref maxBottom
            );
            ExpandTerrainBoundsForTile(
                request,
                maxTileX,
                minTileY,
                halfTileWidth,
                halfTileHeight,
                ref minLeft,
                ref minTop,
                ref maxRight,
                ref maxBottom
            );
            ExpandTerrainBoundsForTile(
                request,
                maxTileX,
                maxTileY,
                halfTileWidth,
                halfTileHeight,
                ref minLeft,
                ref minTop,
                ref maxRight,
                ref maxBottom
            );
        }

        var bounds = new EditorMapFloorRenderBounds(minLeft, minTop, maxRight, maxBottom);
        return bounds.IsValid ? bounds : null;
    }

    private static void ExpandTerrainBoundsForTile(
        EditorMapFloorRenderRequest request,
        int mapTileX,
        int mapTileY,
        double halfTileWidth,
        double halfTileHeight,
        ref double minLeft,
        ref double minTop,
        ref double maxRight,
        ref double maxBottom
    )
    {
        var (centerX, centerY) = ProjectTileCenter(
            request.ViewMode,
            request.TileWidthPixels,
            request.TileHeightPixels,
            mapTileX,
            mapTileY
        );

        minLeft = Math.Min(minLeft, centerX - halfTileWidth);
        minTop = Math.Min(minTop, centerY - halfTileHeight);
        maxRight = Math.Max(maxRight, centerX + halfTileWidth);
        maxBottom = Math.Max(maxBottom, centerY + halfTileHeight);
    }

    private static void ApplySceneBoundsOverride(
        EditorMapFloorRenderRequest request,
        ref double minLeft,
        ref double minTop,
        ref double maxRight,
        ref double maxBottom
    )
    {
        if (request.SceneBoundsOverride is not { IsValid: true } bounds)
            return;

        minLeft = Math.Min(minLeft, bounds.MinLeft);
        minTop = Math.Min(minTop, bounds.MinTop);
        maxRight = Math.Max(maxRight, bounds.MaxRight);
        maxBottom = Math.Max(maxBottom, bounds.MaxBottom);
    }

    private static bool ShouldMaterializeTerrain(
        EditorMapSectorScenePreview sector,
        IReadOnlySet<string>? materializedTerrainSectorPaths
    )
    {
        if (materializedTerrainSectorPaths is null)
            return true;

        return materializedTerrainSectorPaths.Contains(ArcNET.Core.VirtualPath.Normalize(sector.AssetPath));
    }

    private static bool ItemBelongsToSector<T>(T item, IReadOnlySet<string> sectorAssetPaths) =>
        item switch
        {
            EditorMapFloorTileRenderItem t => sectorAssetPaths.Contains(t.SectorAssetPath),
            EditorMapObjectRenderItem o => sectorAssetPaths.Contains(o.SectorAssetPath),
            EditorMapTileOverlayRenderItem ol => sectorAssetPaths.Contains(ol.SectorAssetPath),
            EditorMapRoofRenderItem r => sectorAssetPaths.Contains(r.SectorAssetPath),
            EditorMapObjectAuxiliaryRenderItem a => sectorAssetPaths.Contains(a.SectorAssetPath),
            EditorMapLightRenderItem l => sectorAssetPaths.Contains(l.SectorAssetPath),
            _ => false,
        };

    private static EditorMapFloorRenderRequest CreateDeltaFallbackRequest(
        EditorMapFloorRenderPreview existingPreview,
        EditorMapScenePreview scenePreview
    )
    {
        ArgumentNullException.ThrowIfNull(existingPreview);
        ArgumentNullException.ThrowIfNull(scenePreview);

        var sectorTileCount =
            scenePreview.Sectors.Count == 0
                ? 0
                : checked(
                    scenePreview.Sectors.Count * scenePreview.Sectors[0].TileWidth * scenePreview.Sectors[0].TileHeight
                );

        return new EditorMapFloorRenderRequest
        {
            ViewMode = existingPreview.ViewMode,
            TileWidthPixels = existingPreview.TileWidthPixels,
            TileHeightPixels = existingPreview.TileHeightPixels,
            IncludeEmptyTiles = sectorTileCount > 0 && existingPreview.Tiles.Count == sectorTileCount,
            IncludeObjects = true,
            IncludeRoofs = true,
            IncludeBlockedTileOverlays = true,
            IncludeLightOverlays = true,
            IncludeScriptOverlays = true,
            IncludeJumpPointOverlays = true,
            IncludeEditorObjectStateTint = existingPreview.IncludeEditorObjectStateTint,
            IncludeFloorLightTint = existingPreview.IncludeFloorLightTint,
            AmbientLighting = existingPreview.AmbientLighting,
        };
    }

    private static bool CanApplyDelta(EditorMapFloorRenderPreview existingPreview, EditorMapFloorRenderRequest request)
    {
        ArgumentNullException.ThrowIfNull(existingPreview);
        ArgumentNullException.ThrowIfNull(request);

        return existingPreview.ViewMode == request.ViewMode
            && existingPreview.TileWidthPixels == request.TileWidthPixels
            && existingPreview.TileHeightPixels == request.TileHeightPixels
            && existingPreview.IncludeEditorObjectStateTint == request.IncludeEditorObjectStateTint
            && existingPreview.IncludeFloorLightTint == request.IncludeFloorLightTint
            && EditorMapAmbientLightingBuilder.AreEquivalent(existingPreview.AmbientLighting, request.AmbientLighting);
    }

    private static HashSet<string> GetAffectedSectorAssetPaths(
        IReadOnlyList<EditorMapSectorScenePreview> sectors,
        string changedSectorAssetPath
    )
    {
        ArgumentNullException.ThrowIfNull(sectors);
        ArgumentException.ThrowIfNullOrWhiteSpace(changedSectorAssetPath);

        var sectorsByCoordinate = sectors.ToDictionary(static sector => (sector.LocalX, sector.LocalY));
        var changedSector = sectors.FirstOrDefault(sector =>
            string.Equals(
                ArcNET.Core.VirtualPath.Normalize(sector.AssetPath),
                ArcNET.Core.VirtualPath.Normalize(changedSectorAssetPath),
                StringComparison.OrdinalIgnoreCase
            )
        );
        if (changedSector is null)
            return [];

        var affectedSectorAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var localY = changedSector.LocalY - 1; localY <= changedSector.LocalY + 1; localY++)
        {
            for (var localX = changedSector.LocalX - 1; localX <= changedSector.LocalX + 1; localX++)
            {
                if (sectorsByCoordinate.TryGetValue((localX, localY), out var sector))
                    affectedSectorAssetPaths.Add(sector.AssetPath);
            }
        }

        return affectedSectorAssetPaths;
    }

    private readonly record struct DeltaObjectSourceInfo(int SameTileOrder, bool IsDead);

    private static Dictionary<GameObjectGuid, DeltaObjectSourceInfo> BuildObjectSourceLookup(
        IReadOnlyList<EditorMapSectorScenePreview> sectors,
        HashSet<string> affectedSectorAssetPaths
    )
    {
        var objectLookup = new Dictionary<GameObjectGuid, DeltaObjectSourceInfo>();

        for (var sectorIndex = 0; sectorIndex < sectors.Count; sectorIndex++)
        {
            var sector = sectors[sectorIndex];
            if (!affectedSectorAssetPaths.Contains(sector.AssetPath))
                continue;

            var sameTileOrders = BuildCeSameTileOrders(sector.Objects);
            for (var objectIndex = 0; objectIndex < sector.Objects.Count; objectIndex++)
            {
                var obj = sector.Objects[objectIndex];
                objectLookup[obj.ObjectId] = new DeltaObjectSourceInfo(sameTileOrders[objectIndex], obj.IsDead);
            }
        }

        return objectLookup;
    }

    private static RawTileRenderItem RehydrateRawTile(
        EditorMapFloorTileRenderItem tile,
        EditorMapFloorRenderPreview existingPreview,
        EditorMapFloorRenderRequest request,
        int mapTileWidth
    ) =>
        new(
            SectorAssetPath: tile.SectorAssetPath,
            MapTileX: tile.MapTileX,
            MapTileY: tile.MapTileY,
            Tile: tile.Tile,
            ArtId: tile.ArtId,
            IsBlocked: tile.IsBlocked,
            HasLight: tile.HasLight,
            HasScript: tile.HasScript,
            DrawOrder: GetDrawOrder(request.ViewMode, mapTileWidth, tile.MapTileX, tile.MapTileY),
            CenterX: tile.CenterX - existingPreview.OffsetX,
            CenterY: tile.CenterY - existingPreview.OffsetY
        );

    private static RawTileOverlayRenderItem RehydrateRawTileOverlay(
        EditorMapTileOverlayRenderItem overlay,
        EditorMapFloorRenderPreview existingPreview,
        EditorMapFloorRenderRequest request,
        int mapTileWidth
    )
    {
        var tileDrawOrder = GetDrawOrder(request.ViewMode, mapTileWidth, overlay.MapTileX, overlay.MapTileY);
        return new RawTileOverlayRenderItem(
            SectorAssetPath: overlay.SectorAssetPath,
            MapTileX: overlay.MapTileX,
            MapTileY: overlay.MapTileY,
            Tile: overlay.Tile,
            Kind: overlay.Kind,
            SortKey: GetTileOverlaySortKey(tileDrawOrder, overlay.Kind),
            CenterX: overlay.CenterX - existingPreview.OffsetX,
            CenterY: overlay.CenterY - existingPreview.OffsetY,
            SuggestedOpacity: overlay.SuggestedOpacity,
            SuggestedTintColor: overlay.SuggestedTintColor
        );
    }

    private static RawObjectRenderItem RehydrateRawObject(
        EditorMapObjectRenderItem obj,
        EditorMapFloorRenderPreview existingPreview,
        EditorMapFloorRenderRequest request,
        int mapTileWidth,
        IReadOnlyDictionary<GameObjectGuid, DeltaObjectSourceInfo> objectSourceLookup
    )
    {
        int sameTileOrder;
        bool isDead;

        if (objectSourceLookup.TryGetValue(obj.ObjectId, out var sourceInfo))
        {
            sameTileOrder = sourceInfo.SameTileOrder;
            isDead = sourceInfo.IsDead;
        }
        else
        {
            sameTileOrder = obj.SameTileOrder;
            isDead = obj.IsDead;
        }

        return new RawObjectRenderItem(
            SectorAssetPath: obj.SectorAssetPath,
            SourceObjectIndex: obj.SourceObjectIndex,
            ObjectId: obj.ObjectId,
            ProtoId: obj.ProtoId,
            ObjectType: obj.ObjectType,
            CurrentArtId: obj.CurrentArtId,
            Flags: obj.Flags,
            WallFlags: obj.WallFlags,
            SceneryFlags: obj.SceneryFlags,
            MapTileX: obj.MapTileX,
            MapTileY: obj.MapTileY,
            Tile: obj.Tile,
            BaseTileDrawOrder: GetDrawOrder(request.ViewMode, mapTileWidth, obj.MapTileX, obj.MapTileY),
            SameTileOrder: sameTileOrder,
            AnchorX: obj.AnchorX - existingPreview.OffsetX,
            AnchorY: obj.AnchorY - existingPreview.OffsetY,
            SpriteBounds: obj.SpriteBounds,
            IsTileGridSnapped: obj.IsTileGridSnapped,
            Rotation: obj.Rotation,
            RotationIndex: obj.RotationIndex,
            BlitScale: obj.BlitScale,
            BlitFlags: obj.BlitFlags,
            BlitColor: obj.BlitColor,
            BlitAlpha: obj.BlitAlpha,
            IsShrunk: obj.IsShrunk,
            RotationPitch: obj.RotationPitch,
            IsDead: isDead,
            IsRoofCovered: obj.IsRoofCovered,
            IsIndoorTile: obj.IsIndoorTile,
            LightFlags: obj.LightFlags,
            LightAid: obj.LightAid,
            LightColor: obj.LightColor
        );
    }

    private static RawRoofRenderItem RehydrateRawRoof(
        EditorMapRoofRenderItem roof,
        EditorMapFloorRenderPreview existingPreview,
        EditorMapFloorRenderRequest request,
        int mapTileWidth
    )
    {
        var sortMapTileX = roof.MapTileX + 3;
        var sortMapTileY = roof.MapTileY + 3;
        return new RawRoofRenderItem(
            SectorAssetPath: roof.SectorAssetPath,
            RoofCell: roof.RoofCell,
            MapTileX: roof.MapTileX,
            MapTileY: roof.MapTileY,
            ArtId: roof.ArtId,
            BaseTileDrawOrder: GetDrawOrder(request.ViewMode, mapTileWidth, sortMapTileX, sortMapTileY),
            AnchorX: roof.AnchorX - existingPreview.OffsetX,
            AnchorY: roof.AnchorY - existingPreview.OffsetY
        );
    }

    private static RawAuxiliaryRenderItem RehydrateRawAuxiliary(
        EditorMapObjectAuxiliaryRenderItem auxiliary,
        EditorMapFloorRenderPreview existingPreview,
        EditorMapFloorRenderRequest request,
        int mapTileWidth,
        IReadOnlyDictionary<GameObjectGuid, DeltaObjectSourceInfo> objectSourceLookup,
        IDictionary<(GameObjectGuid ParentObjectId, EditorMapObjectAuxiliaryRenderLayer Layer), int> slotOrdinals
    )
    {
        int parentSameTileOrder;
        bool isParentDead;

        if (objectSourceLookup.TryGetValue(auxiliary.ParentObjectId, out var sourceInfo))
        {
            parentSameTileOrder = sourceInfo.SameTileOrder;
            isParentDead = sourceInfo.IsDead;
        }
        else if (
            existingPreview.TryGetObject(auxiliary.ParentObjectId, out var parentObject) && parentObject is not null
        )
        {
            parentSameTileOrder = parentObject.SameTileOrder;
            isParentDead = parentObject.IsDead;
        }
        else
        {
            parentSameTileOrder = 0;
            isParentDead = false;
        }

        var slotKey = (auxiliary.ParentObjectId, auxiliary.Layer);
        var slotOrder = slotOrdinals.TryGetValue(slotKey, out var existingSlotOrder) ? existingSlotOrder : 0;
        slotOrdinals[slotKey] = slotOrder + 1;

        return new RawAuxiliaryRenderItem(
            SectorAssetPath: auxiliary.SectorAssetPath,
            ParentObjectId: auxiliary.ParentObjectId,
            ParentObjectType: auxiliary.ParentObjectType,
            CommittedRenderLayer: auxiliary.CommittedRenderLayer,
            ArtId: auxiliary.ArtId,
            Layer: auxiliary.Layer,
            SlotOrder: slotOrder,
            MapTileX: auxiliary.MapTileX,
            MapTileY: auxiliary.MapTileY,
            Tile: auxiliary.Tile,
            ParentBaseTileDrawOrder: GetDrawOrder(
                request.ViewMode,
                mapTileWidth,
                auxiliary.MapTileX,
                auxiliary.MapTileY
            ),
            ParentSameTileOrder: parentSameTileOrder,
            AnchorX: auxiliary.AnchorX - existingPreview.OffsetX,
            AnchorY: auxiliary.AnchorY - existingPreview.OffsetY,
            RotationIndex: auxiliary.RotationIndex,
            ScalePercent: auxiliary.ScalePercent,
            IsShrunk: auxiliary.IsShrunk,
            IsParentDead: isParentDead,
            IsRoofCovered: auxiliary.IsRoofCovered,
            SuggestedTintColor: auxiliary.SuggestedTintColor,
            BlendMode: auxiliary.BlendMode
        );
    }

    private static EditorMapLightRenderItem RehydrateRawLight(
        EditorMapLightRenderItem light,
        EditorMapFloorRenderPreview existingPreview
    ) =>
        new()
        {
            SectorAssetPath = light.SectorAssetPath,
            MapTileX = light.MapTileX,
            MapTileY = light.MapTileY,
            Tile = light.Tile,
            ArtId = light.ArtId,
            DrawOrder = 0,
            AnchorX = light.AnchorX - existingPreview.OffsetX,
            AnchorY = light.AnchorY - existingPreview.OffsetY,
            SuggestedTintColor = light.SuggestedTintColor,
            SuggestedOpacity = light.SuggestedOpacity,
            Flags = light.Flags,
        };

    private static void ExpandRetainedTileBounds(
        EditorMapFloorTileRenderItem tile,
        EditorMapFloorRenderPreview existingPreview,
        EditorMapFloorRenderRequest request,
        ref double minLeft,
        ref double minTop,
        ref double maxRight,
        ref double maxBottom
    )
    {
        var halfTileWidth = request.TileWidthPixels / 2d;
        var halfTileHeight = request.TileHeightPixels / 2d;
        var centerX = tile.CenterX - existingPreview.OffsetX;
        var centerY = tile.CenterY - existingPreview.OffsetY;

        minLeft = Math.Min(minLeft, centerX - halfTileWidth);
        minTop = Math.Min(minTop, centerY - halfTileHeight);
        maxRight = Math.Max(maxRight, centerX + halfTileWidth);
        maxBottom = Math.Max(maxBottom, centerY + halfTileHeight);
    }

    private static void ExpandRetainedObjectBounds(
        EditorMapObjectRenderItem obj,
        EditorMapFloorRenderPreview existingPreview,
        EditorMapFloorRenderRequest request,
        ref double minLeft,
        ref double minTop,
        ref double maxRight,
        ref double maxBottom
    )
    {
        var scaleX = request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileWidthPixels / 80d : 1d;
        var scaleY = request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileHeightPixels / 40d : 1d;
        var anchorX = obj.AnchorX - existingPreview.OffsetX;
        var anchorY = obj.AnchorY - existingPreview.OffsetY;

        if (obj.SpriteBounds is not { } spriteBounds)
        {
            minLeft = Math.Min(minLeft, anchorX);
            minTop = Math.Min(minTop, anchorY);
            maxRight = Math.Max(maxRight, anchorX);
            maxBottom = Math.Max(maxBottom, anchorY);
            return;
        }

        var (centerX, centerY) = GetLayoutSpriteCenter(obj.ObjectType, obj.CurrentArtId, spriteBounds);
        var left = anchorX - (centerX * scaleX);
        var top = anchorY - (centerY * scaleY);
        var right = left + (spriteBounds.MaxFrameWidth * scaleX);
        var bottom = top + (spriteBounds.MaxFrameHeight * scaleY);

        minLeft = Math.Min(minLeft, left);
        minTop = Math.Min(minTop, top);
        maxRight = Math.Max(maxRight, right);
        maxBottom = Math.Max(maxBottom, bottom);
    }

    private static void ExpandRetainedRoofBounds(
        EditorMapRoofRenderItem roof,
        EditorMapFloorRenderPreview existingPreview,
        EditorMapFloorRenderRequest request,
        ref double minLeft,
        ref double minTop,
        ref double maxRight,
        ref double maxBottom
    ) =>
        ExpandRoofBounds(
            request.ViewMode,
            request.TileWidthPixels,
            request.TileHeightPixels,
            roof.AnchorX - existingPreview.OffsetX,
            roof.AnchorY - existingPreview.OffsetY,
            ref minLeft,
            ref minTop,
            ref maxRight,
            ref maxBottom
        );

    private static EditorMapCommittedRenderLayer GetCommittedRenderLayer(ObjectType objectType, ObjectFlags flags) =>
        flags.HasFlag(ObjectFlags.Flat)
            ? EditorMapCommittedRenderLayer.GroundDecal
            : objectType switch
            {
                ObjectType.Wall => EditorMapCommittedRenderLayer.Wall,
                ObjectType.Portal => EditorMapCommittedRenderLayer.Wall,
                ObjectType.Scenery => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Container => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Pc => EditorMapCommittedRenderLayer.Mobile,
                ObjectType.Npc => EditorMapCommittedRenderLayer.Mobile,
                ObjectType.Projectile => EditorMapCommittedRenderLayer.Mobile,
                ObjectType.Weapon => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Ammo => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Armor => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Gold => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Food => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Scroll => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Key => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.KeyRing => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Written => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Generic => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Trap => EditorMapCommittedRenderLayer.Scenery,
                _ => EditorMapCommittedRenderLayer.Ground,
            };

    private static void GenerateAuxiliaryItems(
        string sectorAssetPath,
        EditorMapObjectPreview obj,
        EditorMapCommittedRenderLayer committedLayer,
        int mapTileX,
        int mapTileY,
        Location tile,
        double anchorX,
        double anchorY,
        long parentBaseTileDrawOrder,
        int parentSameTileOrder,
        SectorAccumulator local,
        bool isRoofCovered
    )
    {
        var rotationIndex = obj.RotationIndex;
        var scalePercent = obj.BlitScale;
        var isShrunk = obj.IsShrunk;

        for (var i = 0; i < obj.UnderlayArtIds.Count; i++)
        {
            var artId = new ArtId(unchecked((uint)obj.UnderlayArtIds[i]));
            if (artId.Value == 0)
                continue;

            var isReactionUnderlay = artId.Value == 433;
            uint? suggestedTintColor = isReactionUnderlay ? obj.ReactionColor : null;
            var blendMode = EditorMapSpriteBlendMode.SourceOver;

            if (artId.Type is ArtId.TypeCode.Light)
            {
                blendMode = EditorMapSpriteBlendMode.Add;
                suggestedTintColor = obj.LightColor is not null
                    ? 0xFF000000u
                        | ((uint)obj.LightColor.Value.R << 16)
                        | ((uint)obj.LightColor.Value.G << 8)
                        | (uint)obj.LightColor.Value.B
                    : 0xFFFFFFFFu;
            }

            local.RawAuxiliaries.Add(
                new RawAuxiliaryRenderItem(
                    SectorAssetPath: sectorAssetPath,
                    ParentObjectId: obj.ObjectId,
                    ParentObjectType: obj.ObjectType,
                    CommittedRenderLayer: committedLayer,
                    ArtId: artId,
                    Layer: EditorMapObjectAuxiliaryRenderLayer.Underlay,
                    SlotOrder: i,
                    MapTileX: mapTileX,
                    MapTileY: mapTileY,
                    Tile: tile,
                    ParentBaseTileDrawOrder: parentBaseTileDrawOrder,
                    ParentSameTileOrder: parentSameTileOrder,
                    AnchorX: anchorX,
                    AnchorY: anchorY,
                    RotationIndex: rotationIndex,
                    ScalePercent: isReactionUnderlay ? 100 : scalePercent,
                    IsShrunk: !isReactionUnderlay && isShrunk,
                    IsParentDead: obj.IsDead,
                    IsRoofCovered: isRoofCovered,
                    SuggestedTintColor: suggestedTintColor,
                    BlendMode: blendMode
                )
            );
        }

        if (!obj.IsFlat && obj.ShadowArtId.Value != 0)
        {
            local.RawAuxiliaries.Add(
                new RawAuxiliaryRenderItem(
                    SectorAssetPath: sectorAssetPath,
                    ParentObjectId: obj.ObjectId,
                    ParentObjectType: obj.ObjectType,
                    CommittedRenderLayer: committedLayer,
                    ArtId: obj.ShadowArtId,
                    Layer: EditorMapObjectAuxiliaryRenderLayer.Shadow,
                    SlotOrder: 0,
                    MapTileX: mapTileX,
                    MapTileY: mapTileY,
                    Tile: tile,
                    ParentBaseTileDrawOrder: parentBaseTileDrawOrder,
                    ParentSameTileOrder: parentSameTileOrder,
                    AnchorX: anchorX,
                    AnchorY: anchorY,
                    RotationIndex: rotationIndex,
                    ScalePercent: scalePercent,
                    IsShrunk: isShrunk,
                    IsParentDead: obj.IsDead,
                    IsRoofCovered: isRoofCovered,
                    SuggestedTintColor: obj.IsWading ? 0xFF5C5C5C : null,
                    BlendMode: EditorMapSpriteBlendMode.Subtract
                )
            );
        }

        var overlaySlotOrder = 0;
        var overlaySlotCount = Math.Max(obj.OverlayForeArtIds.Count, obj.OverlayBackArtIds.Count);
        for (var slotIndex = overlaySlotCount - 1; slotIndex >= 0; slotIndex--)
        {
            var foreArtId =
                slotIndex < obj.OverlayForeArtIds.Count
                    ? new ArtId(unchecked((uint)obj.OverlayForeArtIds[slotIndex]))
                    : default;
            if (foreArtId.Value != 0)
            {
                uint? suggestedTintColor = null;
                var blendMode = EditorMapSpriteBlendMode.SourceOver;

                if (foreArtId.Type is ArtId.TypeCode.Light)
                {
                    blendMode = EditorMapSpriteBlendMode.Add;
                    suggestedTintColor = obj.LightColor is not null
                        ? 0xFF000000u
                            | ((uint)obj.LightColor.Value.R << 16)
                            | ((uint)obj.LightColor.Value.G << 8)
                            | (uint)obj.LightColor.Value.B
                        : 0xFFFFFFFFu;
                }

                local.RawAuxiliaries.Add(
                    new RawAuxiliaryRenderItem(
                        SectorAssetPath: sectorAssetPath,
                        ParentObjectId: obj.ObjectId,
                        ParentObjectType: obj.ObjectType,
                        CommittedRenderLayer: committedLayer,
                        ArtId: foreArtId,
                        Layer: EditorMapObjectAuxiliaryRenderLayer.OverlayFore,
                        SlotOrder: overlaySlotOrder++,
                        MapTileX: mapTileX,
                        MapTileY: mapTileY,
                        Tile: tile,
                        ParentBaseTileDrawOrder: parentBaseTileDrawOrder,
                        ParentSameTileOrder: parentSameTileOrder,
                        AnchorX: anchorX,
                        AnchorY: anchorY,
                        RotationIndex: rotationIndex,
                        ScalePercent: scalePercent,
                        IsShrunk: isShrunk,
                        IsParentDead: obj.IsDead,
                        IsRoofCovered: isRoofCovered,
                        SuggestedTintColor: suggestedTintColor,
                        BlendMode: blendMode
                    )
                );
            }

            var backArtId =
                slotIndex < obj.OverlayBackArtIds.Count
                    ? new ArtId(unchecked((uint)obj.OverlayBackArtIds[slotIndex]))
                    : default;
            if (backArtId.Value == 0)
                continue;

            uint? backSuggestedTintColor = null;
            var backBlendMode = EditorMapSpriteBlendMode.SourceOver;

            if (backArtId.Type is ArtId.TypeCode.Light)
            {
                backBlendMode = EditorMapSpriteBlendMode.Add;
                backSuggestedTintColor = obj.LightColor is not null
                    ? 0xFF000000u
                        | ((uint)obj.LightColor.Value.R << 16)
                        | ((uint)obj.LightColor.Value.G << 8)
                        | (uint)obj.LightColor.Value.B
                    : 0xFFFFFFFFu;
            }

            local.RawAuxiliaries.Add(
                new RawAuxiliaryRenderItem(
                    SectorAssetPath: sectorAssetPath,
                    ParentObjectId: obj.ObjectId,
                    ParentObjectType: obj.ObjectType,
                    CommittedRenderLayer: committedLayer,
                    ArtId: backArtId,
                    Layer: EditorMapObjectAuxiliaryRenderLayer.OverlayBack,
                    SlotOrder: overlaySlotOrder++,
                    MapTileX: mapTileX,
                    MapTileY: mapTileY,
                    Tile: tile,
                    ParentBaseTileDrawOrder: parentBaseTileDrawOrder,
                    ParentSameTileOrder: parentSameTileOrder,
                    AnchorX: anchorX,
                    AnchorY: anchorY,
                    RotationIndex: rotationIndex,
                    ScalePercent: scalePercent,
                    IsShrunk: isShrunk,
                    IsParentDead: obj.IsDead,
                    IsRoofCovered: isRoofCovered,
                    SuggestedTintColor: backSuggestedTintColor,
                    BlendMode: backBlendMode
                )
            );
        }
    }

    private static void ProcessSector(
        EditorMapSectorScenePreview sector,
        EditorMapFloorRenderRequest request,
        int sectorTileWidth,
        int sectorTileHeight,
        int mapTileWidth,
        SceneSectorLookup sceneSectorLookup,
        bool materializeTerrain,
        SectorAccumulator local
    )
    {
        local.SectorAssetPath ??= sector.AssetPath;
        var halfTileWidth = request.TileWidthPixels / 2d;
        var halfTileHeight = request.TileHeightPixels / 2d;

        var lightTileIndices = sector.LightTileIndices;
        var scriptedTileIndices = sector.ScriptedTileIndices;
        var jumpPointTileIndices = sector.JumpPointTileIndices;

        if (materializeTerrain)
        {
            // Tiles: use precomputed row bitmasks to skip empty rows/columns.
            var tileRowMasks = sector.TileRowMasks;
            for (var tileY = 0; tileY < sectorTileHeight; tileY++)
            {
                var rowMask = tileRowMasks[tileY];
                if (rowMask == 0 && !request.IncludeEmptyTiles)
                    continue;

                if (request.IncludeEmptyTiles)
                {
                    for (var tileX = 0; tileX < sectorTileWidth; tileX++)
                        ProcessTile(
                            sector,
                            request,
                            sectorTileWidth,
                            sectorTileHeight,
                            mapTileWidth,
                            halfTileWidth,
                            halfTileHeight,
                            tileX,
                            tileY,
                            lightTileIndices,
                            scriptedTileIndices,
                            jumpPointTileIndices,
                            local
                        );
                    continue;
                }

                var remaining = rowMask;
                while (remaining != 0)
                {
                    var tileX = BitOperations.TrailingZeroCount(remaining);
                    ProcessTile(
                        sector,
                        request,
                        sectorTileWidth,
                        sectorTileHeight,
                        mapTileWidth,
                        halfTileWidth,
                        halfTileHeight,
                        tileX,
                        tileY,
                        lightTileIndices,
                        scriptedTileIndices,
                        jumpPointTileIndices,
                        local
                    );
                    remaining &= remaining - 1;
                }
            }
        }

        var sectorAmbientColors = ResolveSectorAmbientLightColors(sector.LightSchemeIdx, request.AmbientLighting);

        // Objects.
        if (request.IncludeObjects)
        {
            var sameTileOrders = BuildCeSameTileOrders(sector.Objects);
            for (var objectIndex = 0; objectIndex < sector.Objects.Count; objectIndex++)
            {
                var obj = sector.Objects[objectIndex];
                if (obj.Location is not { } location)
                    continue;

                // CE editor mode shows ALL objects regardless of visibility flags.
                // Do not filter OF_INVISIBLE here — the flag is only meaningful in gameplay mode.

                var mapTileX = checked((sector.LocalX * sectorTileWidth) + location.X);
                var mapTileY = checked((sector.LocalY * sectorTileHeight) + location.Y);
                UpdateSectorMapTileBounds(local, mapTileX, mapTileY);
                var isRoofCovered = request.IncludeRoofs && IsRoofCovered(sceneSectorLookup, mapTileX, mapTileY);

                if (ShouldHideTransparentWallUnderFadedRoof(obj, mapTileX, mapTileY, sceneSectorLookup))
                    continue;

                var baseTileDrawOrder = GetDrawOrder(request.ViewMode, mapTileWidth, mapTileX, mapTileY);
                var (tileCenterX, tileCenterY) = ProjectTileCenter(
                    request.ViewMode,
                    request.TileWidthPixels,
                    request.TileHeightPixels,
                    mapTileX,
                    mapTileY
                );
                var (anchorX, anchorY) = ProjectObjectAnchor(
                    request.ViewMode,
                    request.TileWidthPixels,
                    request.TileHeightPixels,
                    tileCenterX,
                    tileCenterY,
                    obj
                );

                ExpandObjectBounds(
                    request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileWidthPixels / 80d : 1d,
                    request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileHeightPixels / 40d : 1d,
                    obj,
                    anchorX,
                    anchorY,
                    ref local.MinLeft,
                    ref local.MinTop,
                    ref local.MaxRight,
                    ref local.MaxBottom
                );

                var committedLayer = GetCommittedRenderLayer(obj.ObjectType, obj.Flags);
                var isIndoorTile = IsIndoorTileArt(new ArtId(sector.GetTileArtId(location.X, location.Y)));

                local.RawObjects.Add(
                    new RawObjectRenderItem(
                        SectorAssetPath: sector.AssetPath,
                        SourceObjectIndex: obj.SourceObjectIndex,
                        ObjectId: obj.ObjectId,
                        ProtoId: obj.ProtoId,
                        ObjectType: obj.ObjectType,
                        CurrentArtId: obj.CurrentArtId,
                        Flags: obj.Flags,
                        WallFlags: obj.WallFlags,
                        SceneryFlags: obj.SceneryFlags,
                        MapTileX: mapTileX,
                        MapTileY: mapTileY,
                        Tile: location,
                        BaseTileDrawOrder: baseTileDrawOrder,
                        SameTileOrder: sameTileOrders[objectIndex],
                        AnchorX: anchorX,
                        AnchorY: anchorY,
                        SpriteBounds: obj.SpriteBounds,
                        IsTileGridSnapped: obj.IsTileGridSnapped,
                        Rotation: obj.Rotation,
                        RotationIndex: obj.RotationIndex,
                        BlitScale: obj.BlitScale,
                        BlitFlags: obj.BlitFlags,
                        BlitColor: obj.BlitColor,
                        BlitAlpha: obj.BlitAlpha,
                        IsShrunk: obj.IsShrunk,
                        RotationPitch: obj.RotationPitch,
                        IsDead: obj.IsDead,
                        IsRoofCovered: isRoofCovered,
                        IsIndoorTile: isIndoorTile,
                        LightFlags: obj.LightFlags,
                        LightAid: obj.LightAid,
                        LightColor: obj.LightColor
                    )
                );

                // Generate auxiliary layer items (underlays, shadows, overlays).
                // CE sub_443620() applies rect->y += 15 for wading objects to ALL eye candy rects.
                // Scale the 15px CE offset to ArcNET's tile height.
                var auxiliaryAnchorY =
                    obj.IsWading && !obj.Flags.HasFlag(ObjectFlags.WaterWalking)
                        ? anchorY + (15d * request.TileHeightPixels / 40d)
                        : anchorY;
                GenerateAuxiliaryItems(
                    sector.AssetPath,
                    obj,
                    committedLayer,
                    mapTileX,
                    mapTileY,
                    location,
                    anchorX,
                    auxiliaryAnchorY,
                    baseTileDrawOrder,
                    sameTileOrders[objectIndex],
                    local,
                    isRoofCovered
                );

                if (request.IncludeLightOverlays)
                {
                    if (obj.LightAid.Value != 0)
                    {
                        var scaleX =
                            request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileWidthPixels / 80d : 1d;
                        var scaleY =
                            request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileHeightPixels / 40d : 1d;
                        double lightCenterX = tileCenterX + (obj.OffsetX * scaleX);
                        double lightCenterY = tileCenterY + (obj.OffsetY * scaleY);
                        var suggestedTintColor = ResolveProjectedLightTint(
                            obj.LightColor is not null ? PackOpaqueColor(obj.LightColor.Value) : 0xFFFFFFFFu,
                            (SectorLightFlags)obj.LightFlags,
                            sectorAmbientColors
                        );

                        local.RawLights.Add(
                            new EditorMapLightRenderItem
                            {
                                SectorAssetPath = sector.AssetPath,
                                MapTileX = mapTileX,
                                MapTileY = mapTileY,
                                Tile = location,
                                ArtId = obj.LightAid,
                                DrawOrder = 0,
                                AnchorX = lightCenterX,
                                AnchorY = lightCenterY,
                                SuggestedTintColor = suggestedTintColor,
                                SuggestedOpacity = 0.4d,
                                Flags = (SectorLightFlags)obj.LightFlags,
                            }
                        );
                    }

                    for (var i = 0; i < obj.OverlayLights.Count; i++)
                    {
                        var overlayLight = obj.OverlayLights[i];
                        var scaleX =
                            request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileWidthPixels / 80d : 1d;
                        var scaleY =
                            request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileHeightPixels / 40d : 1d;
                        double lightCenterX = tileCenterX + (obj.OffsetX * scaleX);
                        double lightCenterY = tileCenterY + (obj.OffsetY * scaleY);
                        var suggestedTintColor = ResolveProjectedLightTint(
                            overlayLight.Color is not null ? PackOpaqueColor(overlayLight.Color.Value) : 0xFFFFFFFFu,
                            (SectorLightFlags)overlayLight.Flags,
                            sectorAmbientColors
                        );

                        local.RawLights.Add(
                            new EditorMapLightRenderItem
                            {
                                SectorAssetPath = sector.AssetPath,
                                MapTileX = mapTileX,
                                MapTileY = mapTileY,
                                Tile = location,
                                ArtId = overlayLight.ArtId,
                                DrawOrder = 0,
                                AnchorX = lightCenterX,
                                AnchorY = lightCenterY,
                                SuggestedTintColor = suggestedTintColor,
                                SuggestedOpacity = 0.4d,
                                Flags = (SectorLightFlags)overlayLight.Flags,
                            }
                        );
                    }
                }
            }
        }

        // Roofs: use precomputed row bitmasks.
        if (materializeTerrain && request.IncludeRoofs && sector.RoofArtIds is not null)
        {
            var roofRowMasks = sector.RoofRowMasks;
            for (var roofY = 0; roofY < sector.RoofHeight; roofY++)
            {
                if (roofRowMasks is not null)
                {
                    var rowMask = roofRowMasks[roofY];
                    if (rowMask == 0)
                        continue;

                    var remaining = rowMask;
                    while (remaining != 0)
                    {
                        var roofX = BitOperations.TrailingZeroCount(remaining);
                        ProcessRoof(
                            sector,
                            request,
                            sectorTileWidth,
                            sectorTileHeight,
                            mapTileWidth,
                            roofX,
                            roofY,
                            local
                        );
                        remaining &= remaining - 1;
                    }
                }
                else
                {
                    for (var roofX = 0; roofX < sector.RoofWidth; roofX++)
                        ProcessRoof(
                            sector,
                            request,
                            sectorTileWidth,
                            sectorTileHeight,
                            mapTileWidth,
                            roofX,
                            roofY,
                            local
                        );
                }
            }
        }

        // Sector lights
        if (materializeTerrain && request.IncludeLightOverlays)
        {
            for (var i = 0; i < sector.Lights.Count; i++)
            {
                var lightPreview = sector.Lights[i];
                var localTileX = lightPreview.TileX % 64;
                var localTileY = lightPreview.TileY % 64;
                var mapTileX = checked((sector.LocalX * sectorTileWidth) + localTileX);
                var mapTileY = checked((sector.LocalY * sectorTileHeight) + localTileY);
                UpdateSectorMapTileBounds(local, mapTileX, mapTileY);

                var (tileCenterX, tileCenterY) = ProjectTileCenter(
                    request.ViewMode,
                    request.TileWidthPixels,
                    request.TileHeightPixels,
                    mapTileX,
                    mapTileY
                );

                var scaleX = request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileWidthPixels / 80d : 1d;
                var scaleY = request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileHeightPixels / 40d : 1d;
                double lightCenterX = tileCenterX + (lightPreview.OffsetX * scaleX);
                double lightCenterY = tileCenterY + (lightPreview.OffsetY * scaleY);

                local.RawLights.Add(
                    new EditorMapLightRenderItem
                    {
                        SectorAssetPath = sector.AssetPath,
                        MapTileX = mapTileX,
                        MapTileY = mapTileY,
                        Tile = new Location(checked((short)localTileX), checked((short)localTileY)),
                        ArtId = lightPreview.ArtId,
                        DrawOrder = 0,
                        AnchorX = lightCenterX,
                        AnchorY = lightCenterY,
                        SuggestedTintColor = ResolveProjectedLightTint(
                            lightPreview.TintColor | 0xFF000000u,
                            lightPreview.Flags,
                            sectorAmbientColors
                        ),
                        SuggestedOpacity = 0.4d,
                        Flags = lightPreview.Flags,
                    }
                );
            }
        }
    }

    private static void ProcessTile(
        EditorMapSectorScenePreview sector,
        EditorMapFloorRenderRequest request,
        int sectorTileWidth,
        int sectorTileHeight,
        int mapTileWidth,
        double halfTileWidth,
        double halfTileHeight,
        int tileX,
        int tileY,
        HashSet<int> lightTileIndices,
        HashSet<int> scriptedTileIndices,
        HashSet<int> jumpPointTileIndices,
        SectorAccumulator local
    )
    {
        var tileArtId = sector.GetTileArtId(tileX, tileY);
        if (!request.IncludeEmptyTiles && tileArtId == 0)
            return;

        var mapTileX = checked((sector.LocalX * sectorTileWidth) + tileX);
        var mapTileY = checked((sector.LocalY * sectorTileHeight) + tileY);
        UpdateSectorMapTileBounds(local, mapTileX, mapTileY);
        var tileIndex = GetTileIndex(tileX, tileY);
        var drawOrder = GetDrawOrder(request.ViewMode, mapTileWidth, mapTileX, mapTileY);
        var (centerX, centerY) = ProjectTileCenter(
            request.ViewMode,
            request.TileWidthPixels,
            request.TileHeightPixels,
            mapTileX,
            mapTileY
        );

        local.MinLeft = Math.Min(local.MinLeft, centerX - halfTileWidth);
        local.MinTop = Math.Min(local.MinTop, centerY - halfTileHeight);
        local.MaxRight = Math.Max(local.MaxRight, centerX + halfTileWidth);
        local.MaxBottom = Math.Max(local.MaxBottom, centerY + halfTileHeight);

        local.RawTiles.Add(
            new RawTileRenderItem(
                SectorAssetPath: sector.AssetPath,
                MapTileX: mapTileX,
                MapTileY: mapTileY,
                Tile: new Location(checked((short)tileX), checked((short)tileY)),
                ArtId: new ArtId(tileArtId),
                IsBlocked: sector.IsTileBlocked(tileX, tileY),
                HasLight: lightTileIndices.Contains(tileIndex),
                HasScript: scriptedTileIndices.Contains(tileIndex),
                DrawOrder: drawOrder,
                CenterX: centerX,
                CenterY: centerY
            )
        );

        if (sector.IsTileBlocked(tileX, tileY) && request.IncludeBlockedTileOverlays)
        {
            local.RawTileOverlays.Add(
                new RawTileOverlayRenderItem(
                    SectorAssetPath: sector.AssetPath,
                    MapTileX: mapTileX,
                    MapTileY: mapTileY,
                    Tile: new Location(checked((short)tileX), checked((short)tileY)),
                    Kind: EditorMapTileOverlayKind.BlockedTile,
                    SortKey: GetTileOverlaySortKey(drawOrder, EditorMapTileOverlayKind.BlockedTile),
                    CenterX: centerX,
                    CenterY: centerY,
                    SuggestedOpacity: GetTileOverlaySuggestedOpacity(EditorMapTileOverlayKind.BlockedTile),
                    SuggestedTintColor: GetTileOverlaySuggestedTintColor(EditorMapTileOverlayKind.BlockedTile)
                )
            );
        }

        if (lightTileIndices.Contains(tileIndex) && request.IncludeLightOverlays)
        {
            local.RawTileOverlays.Add(
                new RawTileOverlayRenderItem(
                    SectorAssetPath: sector.AssetPath,
                    MapTileX: mapTileX,
                    MapTileY: mapTileY,
                    Tile: new Location(checked((short)tileX), checked((short)tileY)),
                    Kind: EditorMapTileOverlayKind.Light,
                    SortKey: GetTileOverlaySortKey(drawOrder, EditorMapTileOverlayKind.Light),
                    CenterX: centerX,
                    CenterY: centerY,
                    SuggestedOpacity: GetTileOverlaySuggestedOpacity(EditorMapTileOverlayKind.Light),
                    SuggestedTintColor: GetTileOverlaySuggestedTintColor(EditorMapTileOverlayKind.Light)
                )
            );
        }

        if (scriptedTileIndices.Contains(tileIndex) && request.IncludeScriptOverlays)
        {
            local.RawTileOverlays.Add(
                new RawTileOverlayRenderItem(
                    SectorAssetPath: sector.AssetPath,
                    MapTileX: mapTileX,
                    MapTileY: mapTileY,
                    Tile: new Location(checked((short)tileX), checked((short)tileY)),
                    Kind: EditorMapTileOverlayKind.Script,
                    SortKey: GetTileOverlaySortKey(drawOrder, EditorMapTileOverlayKind.Script),
                    CenterX: centerX,
                    CenterY: centerY,
                    SuggestedOpacity: GetTileOverlaySuggestedOpacity(EditorMapTileOverlayKind.Script),
                    SuggestedTintColor: GetTileOverlaySuggestedTintColor(EditorMapTileOverlayKind.Script)
                )
            );
        }

        if (jumpPointTileIndices.Contains(tileIndex) && request.IncludeJumpPointOverlays)
        {
            local.RawTileOverlays.Add(
                new RawTileOverlayRenderItem(
                    SectorAssetPath: sector.AssetPath,
                    MapTileX: mapTileX,
                    MapTileY: mapTileY,
                    Tile: new Location(checked((short)tileX), checked((short)tileY)),
                    Kind: EditorMapTileOverlayKind.JumpPoint,
                    SortKey: GetTileOverlaySortKey(drawOrder, EditorMapTileOverlayKind.JumpPoint),
                    CenterX: centerX,
                    CenterY: centerY,
                    SuggestedOpacity: GetTileOverlaySuggestedOpacity(EditorMapTileOverlayKind.JumpPoint),
                    SuggestedTintColor: GetTileOverlaySuggestedTintColor(EditorMapTileOverlayKind.JumpPoint)
                )
            );
        }
    }

    private static void ProcessRoof(
        EditorMapSectorScenePreview sector,
        EditorMapFloorRenderRequest request,
        int sectorTileWidth,
        int sectorTileHeight,
        int mapTileWidth,
        int roofX,
        int roofY,
        SectorAccumulator local
    )
    {
        var roofArtId = sector.GetRoofArtId(roofX, roofY);
        if (roofArtId is null or 0u or uint.MaxValue)
            return;

        var roof = new ArtId(roofArtId.Value);
        if (roof.IsRoofFill)
            return;

        var mapTileX = checked((sector.LocalX * sectorTileWidth) + (roofX * 4));
        var mapTileY = checked((sector.LocalY * sectorTileHeight) + (roofY * 4));
        UpdateSectorMapTileBounds(local, mapTileX, mapTileY, mapTileX + 3, mapTileY + 3);
        var sortMapTileX = mapTileX + 3;
        var sortMapTileY = mapTileY + 3;
        var baseDrawOrder = GetDrawOrder(request.ViewMode, mapTileWidth, sortMapTileX, sortMapTileY);
        var (anchorX, anchorY) = ProjectRoofAnchor(
            request.ViewMode,
            request.TileWidthPixels,
            request.TileHeightPixels,
            mapTileX,
            mapTileY
        );

        ExpandRoofBounds(
            request.ViewMode,
            request.TileWidthPixels,
            request.TileHeightPixels,
            anchorX,
            anchorY,
            ref local.MinLeft,
            ref local.MinTop,
            ref local.MaxRight,
            ref local.MaxBottom
        );

        local.RawRoofs.Add(
            new RawRoofRenderItem(
                SectorAssetPath: sector.AssetPath,
                RoofCell: new Location(checked((short)roofX), checked((short)roofY)),
                MapTileX: mapTileX,
                MapTileY: mapTileY,
                ArtId: roof,
                BaseTileDrawOrder: baseDrawOrder,
                AnchorX: anchorX,
                AnchorY: anchorY
            )
        );
    }

    private static EditorMapFloorRenderPreview CreateEmptyPreview(
        string mapName,
        EditorMapFloorRenderRequest request
    ) =>
        new()
        {
            MapName = mapName,
            ViewMode = request.ViewMode,
            TileWidthPixels = request.TileWidthPixels,
            TileHeightPixels = request.TileHeightPixels,
            WidthPixels = 0d,
            HeightPixels = 0d,
            SceneRevision = ComputeSceneRevision(mapName, request, [], 0d, 0d),
            Slices = [],
            TileOrderMap = [],
            ObjectOrderMap = [],
            ObjectAuxiliaryOrderMap = [],
            OverlayOrderMap = [],
            LightOrderMap = [],
            RoofOrderMap = [],
            RenderQueueOrderMap = [],
            IncludeEditorObjectStateTint = request.IncludeEditorObjectStateTint,
            IncludeFloorLightTint = request.IncludeFloorLightTint,
            IncludeEmptyTerrainTiles = request.IncludeEmptyTiles,
            IncludeTerrainRoofs = request.IncludeRoofs,
            IncludeTerrainBlockedTileOverlays = request.IncludeBlockedTileOverlays,
            IncludeTerrainLightOverlays = request.IncludeLightOverlays,
            IncludeTerrainScriptOverlays = request.IncludeScriptOverlays,
            IncludeTerrainJumpPointOverlays = request.IncludeJumpPointOverlays,
            AmbientLighting = request.AmbientLighting,
            IsTerrainMaterializationPartial = request.MaterializedTerrainSectorAssetPaths is not null,
            MaterializedTerrainSectorCount = 0,
            TotalTerrainSectorCount = 0,
        };

    private static int[] BuildCeSameTileOrders(IReadOnlyList<EditorMapObjectPreview> objects)
    {
        var sameTileOrders = new int[objects.Count];
        var orderedObjectIndicesByTile = new Dictionary<int, List<int>>();

        for (var objectIndex = 0; objectIndex < objects.Count; objectIndex++)
        {
            var obj = objects[objectIndex];
            if (obj.Location is not { } location)
                continue;

            var tileIndex = GetTileIndex(location.X, location.Y);
            if (!orderedObjectIndicesByTile.TryGetValue(tileIndex, out var objectIndices))
            {
                objectIndices = [];
                orderedObjectIndicesByTile[tileIndex] = objectIndices;
            }

            InsertCeSameTileObject(objectIndices, objects, objectIndex);
        }

        foreach (var objectIndices in orderedObjectIndicesByTile.Values)
        {
            for (var sameTileOrder = 0; sameTileOrder < objectIndices.Count; sameTileOrder++)
                sameTileOrders[objectIndices[sameTileOrder]] = sameTileOrder;
        }

        return sameTileOrders;
    }

    private static void InsertCeSameTileObject(
        IList<int> orderedObjectIndices,
        IReadOnlyList<EditorMapObjectPreview> objects,
        int newObjectIndex
    )
    {
        var newObject = objects[newObjectIndex];
        var (newPrimary, newSecondary) = GetObjectTileOrderComponents(newObject);

        for (var index = 0; index < orderedObjectIndices.Count; index++)
        {
            var existingObject = objects[orderedObjectIndices[index]];

            if (newObject.IsFlat)
            {
                if (!existingObject.IsFlat || newObject.IsUnderAllScenery)
                {
                    orderedObjectIndices.Insert(index, newObjectIndex);
                    return;
                }
            }
            else
            {
                if (newObject.ObjectType is ObjectType.Wall && existingObject.ObjectType is ObjectType.Portal)
                {
                    orderedObjectIndices.Insert(index, newObjectIndex);
                    return;
                }

                if (newObject.ObjectType is ObjectType.Portal && existingObject.ObjectType is ObjectType.Wall)
                {
                    orderedObjectIndices.Insert(index + 1, newObjectIndex);
                    return;
                }
            }

            if (existingObject.IsFlat)
                continue;

            var (existingPrimary, existingSecondary) = GetObjectTileOrderComponents(existingObject);
            if (newPrimary < existingPrimary || (newPrimary == existingPrimary && newSecondary < existingSecondary))
            {
                orderedObjectIndices.Insert(index, newObjectIndex);
                return;
            }
        }

        orderedObjectIndices.Add(newObjectIndex);
    }

    private static bool IsRoofCovered(SceneSectorLookup sceneSectorLookup, int mapTileX, int mapTileY)
    {
        var coveredTileX = mapTileX + 3;
        var coveredTileY = mapTileY + 3;
        if (
            !sceneSectorLookup.TryGetSectorTile(
                coveredTileX,
                coveredTileY,
                out var roofSector,
                out var roofLocalTileX,
                out var roofLocalTileY
            )
        )
        {
            return false;
        }

        var roofArtId = roofSector.GetRoofArtId(roofLocalTileX / 4, roofLocalTileY / 4);
        if (roofArtId is null or 0u or uint.MaxValue)
            return false;

        var roof = new ArtId(roofArtId.Value);
        if (roof.IsRoofFill)
            return false;

        if (roof.FrameIndex is < 0 or >= 13)
            return false;

        var row = PositiveModulo(coveredTileY, 4);
        var col = PositiveModulo(coveredTileX, 4);
        if (roof.IsRoofMirrored)
            col = 3 - col;

        return RoofCoverageMatrix[roof.FrameIndex, row, col];
    }

    private static bool ShouldHideTransparentWallUnderFadedRoof(
        EditorMapObjectPreview objectPreview,
        int mapTileX,
        int mapTileY,
        SceneSectorLookup sceneSectorLookup
    ) =>
        objectPreview.ObjectType is ObjectType.Wall
        && (objectPreview.WallFlags & (WallTransparencyLeft | WallTransparencyRight)) != 0
        && (objectPreview.WallFlags & WallTransDisallow) == 0
        && !UsesNonCardinalWallRotation(objectPreview.CurrentArtId)
        && IsRoofFaded(sceneSectorLookup, mapTileX, mapTileY);

    private static bool IsRoofFaded(SceneSectorLookup sceneSectorLookup, int mapTileX, int mapTileY)
    {
        var coveredTileX = mapTileX + 3;
        var coveredTileY = mapTileY + 3;
        if (
            !sceneSectorLookup.TryGetSectorTile(
                coveredTileX,
                coveredTileY,
                out var sector,
                out var localTileX,
                out var localTileY
            )
        )
            return false;

        var roofArtId = sector.GetRoofArtId(localTileX / 4, localTileY / 4);
        if (roofArtId is null or 0u or uint.MaxValue)
            return false;

        var roof = new ArtId(roofArtId.Value);
        return !roof.IsRoofFill && roof.IsRoofFaded;
    }

    private static bool UsesNonCardinalWallRotation(ArtId artId)
    {
        var rotationIndex = (int)((artId.Value >> 11) & 0x7u);
        return rotationIndex is > 1 and < 6;
    }

    private static int GetAuxiliaryBand(EditorMapObjectAuxiliaryRenderLayer layer) =>
        layer switch
        {
            EditorMapObjectAuxiliaryRenderLayer.Underlay => 0,
            EditorMapObjectAuxiliaryRenderLayer.Shadow => 1,
            _ => 2,
        };

    private static bool IsFlatObject(RawObjectRenderItem item) => item.Flags.HasFlag(ObjectFlags.Flat);

    private static bool IsUnderAllScenery(RawObjectRenderItem item) =>
        item.ObjectType is ObjectType.Scenery && item.SceneryFlags.HasFlag(SceneryFlags.UnderAll);

    private static bool IsGhostOrArmorOverlay(RawAuxiliaryRenderItem item) =>
        (
            item.Layer
            is EditorMapObjectAuxiliaryRenderLayer.OverlayBack
                or EditorMapObjectAuxiliaryRenderLayer.OverlayFore
        )
        && (
            item.ParentObjectType is ObjectType.Armor
            || (item.ParentObjectType is ObjectType.Npc && item.IsParentDead && item.ArtId.ArtNum == 243)
        );

    private readonly record struct NonFlatSortItem(
        EditorMapRenderQueueItemKind Kind,
        int Index,
        long BaseTileDrawOrder,
        int SameTileOrder,
        int MapTileX,
        int MapTileY,
        int SubOrder,
        int SlotOrder
    );

    private readonly record struct RenderQueueSortItem(double SortKey, EditorMapRenderQueueItemKind Kind, int Index);

    private static void SortRawItems(
        List<RawTileRenderItem> rawTiles,
        List<RawTileOverlayRenderItem> rawTileOverlays,
        List<RawObjectRenderItem> rawObjects,
        List<RawRoofRenderItem> rawRoofs,
        List<RawAuxiliaryRenderItem> rawAuxiliaries
    )
    {
        SortRawTiles(rawTiles);

        rawTileOverlays.Sort(
            (a, b) =>
            {
                var cmp = a.SortKey.CompareTo(b.SortKey);
                if (cmp != 0)
                    return cmp;
                cmp = a.MapTileX.CompareTo(b.MapTileX);
                if (cmp != 0)
                    return cmp;
                cmp = a.MapTileY.CompareTo(b.MapTileY);
                return cmp != 0 ? cmp : a.Kind.CompareTo(b.Kind);
            }
        );

        rawObjects.Sort(
            (a, b) =>
            {
                var cmp = a.BaseTileDrawOrder.CompareTo(b.BaseTileDrawOrder);
                if (cmp != 0)
                    return cmp;
                cmp = a.SameTileOrder.CompareTo(b.SameTileOrder);
                if (cmp != 0)
                    return cmp;
                cmp = a.MapTileX.CompareTo(b.MapTileX);
                return cmp != 0 ? cmp : a.MapTileY.CompareTo(b.MapTileY);
            }
        );

        rawRoofs.Sort(
            (a, b) =>
            {
                var cmp = a.BaseTileDrawOrder.CompareTo(b.BaseTileDrawOrder);
                if (cmp != 0)
                    return cmp;
                cmp = a.MapTileX.CompareTo(b.MapTileX);
                return cmp != 0 ? cmp : a.MapTileY.CompareTo(b.MapTileY);
            }
        );

        rawAuxiliaries.Sort(
            (a, b) =>
            {
                var cmp = GetAuxiliaryBand(a.Layer).CompareTo(GetAuxiliaryBand(b.Layer));
                if (cmp != 0)
                    return cmp;
                cmp = a.ParentBaseTileDrawOrder.CompareTo(b.ParentBaseTileDrawOrder);
                if (cmp != 0)
                    return cmp;
                cmp = a.ParentSameTileOrder.CompareTo(b.ParentSameTileOrder);
                if (cmp != 0)
                    return cmp;
                cmp = a.SlotOrder.CompareTo(b.SlotOrder);
                if (cmp != 0)
                    return cmp;
                return a.MapTileX.CompareTo(b.MapTileX);
            }
        );
    }

    private static void SortRawTiles(List<RawTileRenderItem> rawTiles)
    {
        if (rawTiles.Count < 4096 || !TryBucketSortRawTiles(rawTiles))
        {
            rawTiles.Sort(CompareRawTiles);
        }
    }

    private static bool TryBucketSortRawTiles(List<RawTileRenderItem> rawTiles)
    {
        var minBucket = int.MaxValue;
        var maxBucket = int.MinValue;
        for (var index = 0; index < rawTiles.Count; index++)
        {
            var bucket = GetRawTileDrawOrderBucket(rawTiles[index]);
            minBucket = Math.Min(minBucket, bucket);
            maxBucket = Math.Max(maxBucket, bucket);
        }

        var bucketCountLong = (long)maxBucket - minBucket + 1L;
        if (bucketCountLong <= 0L || bucketCountLong > Math.Max(4096, rawTiles.Count))
            return false;

        var buckets = new List<RawTileRenderItem>?[(int)bucketCountLong];
        for (var index = 0; index < rawTiles.Count; index++)
        {
            var tile = rawTiles[index];
            var bucketIndex = GetRawTileDrawOrderBucket(tile) - minBucket;
            (buckets[bucketIndex] ??= []).Add(tile);
        }

        var writeIndex = 0;
        for (var bucketIndex = 0; bucketIndex < buckets.Length; bucketIndex++)
        {
            var bucket = buckets[bucketIndex];
            if (bucket is null)
                continue;

            if (bucket.Count > 1)
                bucket.Sort(CompareRawTiles);

            for (var itemIndex = 0; itemIndex < bucket.Count; itemIndex++)
                rawTiles[writeIndex++] = bucket[itemIndex];
        }

        return writeIndex == rawTiles.Count;
    }

    private static int GetRawTileDrawOrderBucket(RawTileRenderItem tile) => (int)(tile.DrawOrder / 10_000_000L);

    private static int CompareRawTiles(RawTileRenderItem a, RawTileRenderItem b)
    {
        var cmp = a.DrawOrder.CompareTo(b.DrawOrder);
        return cmp != 0 ? cmp : a.MapTileX.CompareTo(b.MapTileX);
    }

    private static uint SampleLightColor(
        double px,
        double py,
        EditorMapAmbientLightColors ambientColors,
        bool isIndoor,
        IReadOnlyList<EditorMapLightRenderItem> lights
    )
    {
        var baseAmbientColor = isIndoor ? ambientColors.Indoor : ambientColors.Outdoor;
        double accumR = baseAmbientColor.R;
        double accumG = baseAmbientColor.G;
        double accumB = baseAmbientColor.B;

        const double radius = 300.0d;

        foreach (var light in lights)
        {
            if (light.Flags.HasFlag(SectorLightFlags.Off))
                continue;

            var dx = px - light.AnchorX;
            var dy = py - light.AnchorY;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance >= radius)
                continue;

            var intensity = 1.0d - (distance / radius);

            var lightColor = light.SuggestedTintColor;
            var r = (double)((lightColor >> 16) & 0xFFu);
            var g = (double)((lightColor >> 8) & 0xFFu);
            var b = (double)(lightColor & 0xFFu);

            if (light.Flags.HasFlag(SectorLightFlags.Dark))
            {
                accumR -= r * intensity;
                accumG -= g * intensity;
                accumB -= b * intensity;
            }
            else
            {
                accumR += r * intensity;
                accumG += g * intensity;
                accumB += b * intensity;
            }
        }

        var finalR = (byte)Math.Clamp(accumR, 0d, 255d);
        var finalG = (byte)Math.Clamp(accumG, 0d, 255d);
        var finalB = (byte)Math.Clamp(accumB, 0d, 255d);

        return 0xFF000000u | ((uint)finalR << 16) | ((uint)finalG << 8) | finalB;
    }

    private static IReadOnlyDictionary<string, EditorMapAmbientLightColors> BuildSectorAmbientLightingLookup(
        IReadOnlyList<EditorMapSectorScenePreview> sectors,
        EditorMapAmbientLightingState? ambientLighting
    )
    {
        var lookup = new Dictionary<string, EditorMapAmbientLightColors>(StringComparer.OrdinalIgnoreCase);
        for (var sectorIndex = 0; sectorIndex < sectors.Count; sectorIndex++)
        {
            var sector = sectors[sectorIndex];
            lookup[sector.AssetPath] = ResolveSectorAmbientLightColors(sector.LightSchemeIdx, ambientLighting);
        }

        return lookup;
    }

    private static EditorMapAmbientLightColors ResolveSectorAmbientLightColors(
        int lightSchemeIndex,
        EditorMapAmbientLightingState? ambientLighting
    ) =>
        ambientLighting?.ResolveForSector(lightSchemeIndex)
        ?? new EditorMapAmbientLightColors(new(128, 128, 128), new(255, 255, 255));

    private static uint ResolveProjectedLightTint(
        uint baseTintColor,
        SectorLightFlags flags,
        EditorMapAmbientLightColors ambientColors
    ) =>
        flags.HasFlag(SectorLightFlags.Indoor)
            ? PackOpaqueColor(ambientColors.Indoor)
            : (flags.HasFlag(SectorLightFlags.Outdoor) ? PackOpaqueColor(ambientColors.Outdoor) : baseTintColor);

    private static uint PackOpaqueColor(Color color) =>
        0xFF000000u | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;

    private static bool IsIndoorTileArt(ArtId artId) => artId.Value == 0 || artId.TileType == 0;

    private static EditorMapFloorRenderPreview BuildResult(
        string mapName,
        EditorMapFloorRenderRequest request,
        IReadOnlyList<EditorMapSectorScenePreview> sourceSectors,
        List<RawTileRenderItem> rawTiles,
        List<RawTileOverlayRenderItem> rawTileOverlays,
        List<RawObjectRenderItem> rawObjects,
        List<RawRoofRenderItem> rawRoofs,
        List<RawAuxiliaryRenderItem> rawAuxiliaries,
        List<EditorMapLightRenderItem> rawLights,
        IReadOnlyDictionary<string, EditorMapAmbientLightColors> ambientLightingBySectorAssetPath,
        IReadOnlyDictionary<string, EditorMapSectorRenderSliceBounds> sectorBoundsByAssetPath,
        double offsetX,
        double offsetY,
        double minLeft,
        double maxRight,
        double minTop,
        double maxBottom,
        int materializedTerrainSectorCount,
        int totalTerrainSectorCount,
        IReadOnlyDictionary<string, long>? preservedSliceRevisionsByAssetPath = null
    )
    {
        var finalLights = new EditorMapLightRenderItem[rawLights.Count];
        for (var i = 0; i < rawLights.Count; i++)
        {
            var l = rawLights[i];
            finalLights[i] = new EditorMapLightRenderItem
            {
                SectorAssetPath = l.SectorAssetPath,
                MapTileX = l.MapTileX,
                MapTileY = l.MapTileY,
                Tile = l.Tile,
                ArtId = l.ArtId,
                DrawOrder = i,
                AnchorX = l.AnchorX + offsetX,
                AnchorY = l.AnchorY + offsetY,
                SuggestedTintColor = l.SuggestedTintColor,
                SuggestedOpacity = l.SuggestedOpacity,
                Flags = l.Flags,
            };
        }

        Array.Sort(
            finalLights,
            (a, b) =>
            {
                var cmp = a.MapTileX.CompareTo(b.MapTileX);
                return cmp != 0 ? cmp : a.MapTileY.CompareTo(b.MapTileY);
            }
        );
        var sliceBuilders = new List<SectorRenderSliceBuilder>();
        var sliceIndexByAssetPath = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        (SectorRenderSliceBuilder Builder, int SliceIndex) GetOrAddSlice(string sectorAssetPath)
        {
            if (sliceIndexByAssetPath.TryGetValue(sectorAssetPath, out var existingIndex))
                return (sliceBuilders[existingIndex], existingIndex);

            var createdIndex = sliceBuilders.Count;
            var builder = new SectorRenderSliceBuilder(sectorAssetPath);
            sliceBuilders.Add(builder);
            sliceIndexByAssetPath[sectorAssetPath] = createdIndex;
            return (builder, createdIndex);
        }

        var tileOrderMap = new uint[rawTiles.Count];
        var halfTileWidth = request.TileWidthPixels / 2d;
        var halfTileHeight = request.TileHeightPixels / 2d;
        for (var i = 0; i < rawTiles.Count; i++)
        {
            var t = rawTiles[i];
            uint? suggestedTintColor = null;
            EditorMapTileLightDiagnostics? lightDiagnostics = null;

            if (request.IncludeFloorLightTint)
            {
                var stepX = halfTileWidth;
                var stepY = halfTileHeight;
                var isIndoor = IsIndoorTileArt(t.ArtId);
                var ambientColors = ambientLightingBySectorAssetPath.TryGetValue(
                    t.SectorAssetPath,
                    out var sectorColors
                )
                    ? sectorColors
                    : ResolveSectorAmbientLightColors(lightSchemeIndex: 0, request.AmbientLighting);

                var tileCenterX = t.CenterX + offsetX;
                var tileCenterY = t.CenterY + offsetY;
                var maxSampleOffset = Math.Sqrt(stepX * stepX + stepY * stepY);
                var activeRadius = 300.0d + maxSampleOffset;
                List<EditorMapLightRenderItem>? activeLights = null;

                for (var lightIndex = 0; lightIndex < finalLights.Length; lightIndex++)
                {
                    var light = finalLights[lightIndex];
                    if (light.Flags.HasFlag(SectorLightFlags.Off))
                        continue;

                    var dx = tileCenterX - light.AnchorX;
                    var dy = tileCenterY - light.AnchorY;
                    var dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist >= activeRadius)
                        continue;

                    activeLights ??= [];
                    activeLights.Add(light);
                }

                var sampledLights = activeLights ?? [];
                var topLeft = SampleLightColor(
                    tileCenterX - stepX,
                    tileCenterY - stepY,
                    ambientColors,
                    isIndoor,
                    sampledLights
                );
                var topCenter = SampleLightColor(
                    tileCenterX,
                    tileCenterY - stepY,
                    ambientColors,
                    isIndoor,
                    sampledLights
                );
                var topRight = SampleLightColor(
                    tileCenterX + stepX,
                    tileCenterY - stepY,
                    ambientColors,
                    isIndoor,
                    sampledLights
                );
                var middleLeft = SampleLightColor(
                    tileCenterX - stepX,
                    tileCenterY,
                    ambientColors,
                    isIndoor,
                    sampledLights
                );
                var middleCenter = SampleLightColor(tileCenterX, tileCenterY, ambientColors, isIndoor, sampledLights);
                var middleRight = SampleLightColor(
                    tileCenterX + stepX,
                    tileCenterY,
                    ambientColors,
                    isIndoor,
                    sampledLights
                );
                var bottomLeft = SampleLightColor(
                    tileCenterX - stepX,
                    tileCenterY + stepY,
                    ambientColors,
                    isIndoor,
                    sampledLights
                );
                var bottomCenter = SampleLightColor(
                    tileCenterX,
                    tileCenterY + stepY,
                    ambientColors,
                    isIndoor,
                    sampledLights
                );
                var bottomRight = SampleLightColor(
                    tileCenterX + stepX,
                    tileCenterY + stepY,
                    ambientColors,
                    isIndoor,
                    sampledLights
                );

                suggestedTintColor = middleCenter;
                lightDiagnostics = new EditorMapTileLightDiagnostics(
                    TopLeft: topLeft,
                    TopCenter: topCenter,
                    TopRight: topRight,
                    MiddleLeft: middleLeft,
                    MiddleCenter: middleCenter,
                    MiddleRight: middleRight,
                    BottomLeft: bottomLeft,
                    BottomCenter: bottomCenter,
                    BottomRight: bottomRight
                );
            }

            var tile = new EditorMapFloorTileRenderItem
            {
                SectorAssetPath = t.SectorAssetPath,
                MapTileX = t.MapTileX,
                MapTileY = t.MapTileY,
                Tile = t.Tile,
                ArtId = t.ArtId,
                IsBlocked = t.IsBlocked,
                HasLight = t.HasLight,
                HasScript = t.HasScript,
                DrawOrder = i,
                CenterX = t.CenterX + offsetX,
                CenterY = t.CenterY + offsetY,
                SuggestedTintColor = suggestedTintColor,
                LightDiagnostics = lightDiagnostics,
            };

            var (sliceBuilder, sliceIndex) = GetOrAddSlice(tile.SectorAssetPath);
            tileOrderMap[i] = EditorMapFloorRenderPreview.PackSliceItemIndex(sliceIndex, sliceBuilder.Tiles.Count);
            sliceBuilder.Tiles.Add(tile);
        }

        var objectOrderMap = new uint[rawObjects.Count];
        for (var i = 0; i < rawObjects.Count; i++)
        {
            var o = rawObjects[i];
            var obj = new EditorMapObjectRenderItem
            {
                SectorAssetPath = o.SectorAssetPath,
                SourceObjectIndex = o.SourceObjectIndex,
                ObjectId = o.ObjectId,
                ProtoId = o.ProtoId,
                ObjectType = o.ObjectType,
                CommittedRenderLayer = GetCommittedRenderLayer(o.ObjectType, o.Flags),
                CurrentArtId = o.CurrentArtId,
                Flags = o.Flags,
                WallFlags = o.WallFlags,
                SceneryFlags = o.SceneryFlags,
                MapTileX = o.MapTileX,
                MapTileY = o.MapTileY,
                Tile = o.Tile,
                DrawOrder = i,
                AnchorX = o.AnchorX + offsetX,
                AnchorY = o.AnchorY + offsetY,
                SpriteBounds = o.SpriteBounds,
                IsTileGridSnapped = o.IsTileGridSnapped,
                Rotation = o.Rotation,
                RotationIndex = o.RotationIndex,
                BlitScale = o.BlitScale,
                BlitFlags = o.BlitFlags,
                BlitColor = o.BlitColor,
                BlitAlpha = o.BlitAlpha,
                IsShrunk = o.IsShrunk,
                RotationPitch = o.RotationPitch,
                IsRoofCovered = o.IsRoofCovered,
                IsIndoorTile = o.IsIndoorTile,
                LightFlags = o.LightFlags,
                LightAid = o.LightAid,
                LightColor = o.LightColor,
                SameTileOrder = o.SameTileOrder,
                IsDead = o.IsDead,
            };

            var (sliceBuilder, sliceIndex) = GetOrAddSlice(obj.SectorAssetPath);
            objectOrderMap[i] = EditorMapFloorRenderPreview.PackSliceItemIndex(sliceIndex, sliceBuilder.Objects.Count);
            sliceBuilder.Objects.Add(obj);
        }

        var overlayOrderMap = new uint[rawTileOverlays.Count];
        for (var i = 0; i < rawTileOverlays.Count; i++)
        {
            var o = rawTileOverlays[i];
            var overlay = new EditorMapTileOverlayRenderItem
            {
                SectorAssetPath = o.SectorAssetPath,
                MapTileX = o.MapTileX,
                MapTileY = o.MapTileY,
                Tile = o.Tile,
                Kind = o.Kind,
                DrawOrder = i,
                CenterX = o.CenterX + offsetX,
                CenterY = o.CenterY + offsetY,
                SuggestedOpacity = o.SuggestedOpacity,
                SuggestedTintColor = o.SuggestedTintColor,
            };

            var (sliceBuilder, sliceIndex) = GetOrAddSlice(overlay.SectorAssetPath);
            overlayOrderMap[i] = EditorMapFloorRenderPreview.PackSliceItemIndex(
                sliceIndex,
                sliceBuilder.Overlays.Count
            );
            sliceBuilder.Overlays.Add(overlay);
        }

        var roofOrderMap = new uint[rawRoofs.Count];
        for (var i = 0; i < rawRoofs.Count; i++)
        {
            var r = rawRoofs[i];
            var roof = new EditorMapRoofRenderItem
            {
                SectorAssetPath = r.SectorAssetPath,
                RoofCell = r.RoofCell,
                MapTileX = r.MapTileX,
                MapTileY = r.MapTileY,
                ArtId = r.ArtId,
                DrawOrder = i,
                AnchorX = r.AnchorX + offsetX,
                AnchorY = r.AnchorY + offsetY,
            };

            var (sliceBuilder, sliceIndex) = GetOrAddSlice(roof.SectorAssetPath);
            roofOrderMap[i] = EditorMapFloorRenderPreview.PackSliceItemIndex(sliceIndex, sliceBuilder.Roofs.Count);
            sliceBuilder.Roofs.Add(roof);
        }

        var objectAuxiliaryOrderMap = new uint[rawAuxiliaries.Count];
        for (var i = 0; i < rawAuxiliaries.Count; i++)
        {
            var a = rawAuxiliaries[i];
            var auxiliary = new EditorMapObjectAuxiliaryRenderItem
            {
                SectorAssetPath = a.SectorAssetPath,
                ParentObjectId = a.ParentObjectId,
                ParentObjectType = a.ParentObjectType,
                CommittedRenderLayer = a.CommittedRenderLayer,
                ArtId = a.ArtId,
                Layer = a.Layer,
                MapTileX = a.MapTileX,
                MapTileY = a.MapTileY,
                Tile = a.Tile,
                DrawOrder = i,
                AnchorX = a.AnchorX + offsetX,
                AnchorY = a.AnchorY + offsetY,
                RotationIndex = a.RotationIndex,
                ScalePercent = a.ScalePercent,
                IsShrunk = a.IsShrunk,
                IsRoofCovered = a.IsRoofCovered,
                SuggestedTintColor = a.SuggestedTintColor,
                BlendMode = a.BlendMode,
            };

            var (sliceBuilder, sliceIndex) = GetOrAddSlice(auxiliary.SectorAssetPath);
            objectAuxiliaryOrderMap[i] = EditorMapFloorRenderPreview.PackSliceItemIndex(
                sliceIndex,
                sliceBuilder.ObjectAuxiliaryItems.Count
            );
            sliceBuilder.ObjectAuxiliaryItems.Add(auxiliary);
        }

        var lightOrderMap = new uint[finalLights.Length];
        for (var i = 0; i < finalLights.Length; i++)
        {
            var light = new EditorMapLightRenderItem
            {
                SectorAssetPath = finalLights[i].SectorAssetPath,
                MapTileX = finalLights[i].MapTileX,
                MapTileY = finalLights[i].MapTileY,
                Tile = finalLights[i].Tile,
                ArtId = finalLights[i].ArtId,
                DrawOrder = i,
                AnchorX = finalLights[i].AnchorX,
                AnchorY = finalLights[i].AnchorY,
                SuggestedTintColor = finalLights[i].SuggestedTintColor,
                SuggestedOpacity = finalLights[i].SuggestedOpacity,
                Flags = finalLights[i].Flags,
            };

            finalLights[i] = light;
            var (sliceBuilder, sliceIndex) = GetOrAddSlice(light.SectorAssetPath);
            lightOrderMap[i] = EditorMapFloorRenderPreview.PackSliceItemIndex(sliceIndex, sliceBuilder.Lights.Count);
            sliceBuilder.Lights.Add(light);
        }

        var renderQueueOrderMap = BuildRenderQueue(
            rawTiles,
            tileOrderMap,
            rawTileOverlays,
            overlayOrderMap,
            rawObjects,
            objectOrderMap,
            rawRoofs,
            roofOrderMap,
            rawAuxiliaries,
            objectAuxiliaryOrderMap,
            finalLights,
            lightOrderMap,
            sliceBuilders
        );

        var slices = new EditorMapSectorRenderSlice[sliceBuilders.Count];
        for (var sliceIndex = 0; sliceIndex < sliceBuilders.Count; sliceIndex++)
        {
            var sliceBuilder = sliceBuilders[sliceIndex];
            var fallbackBounds = sliceBuilder.CreateFallbackBounds(request);
            var bounds = sectorBoundsByAssetPath.TryGetValue(sliceBuilder.SectorAssetPath, out var sectorBounds)
                ? new EditorMapSectorRenderSliceBounds(
                    Left: fallbackBounds.Left,
                    Top: fallbackBounds.Top,
                    Width: fallbackBounds.Width,
                    Height: fallbackBounds.Height,
                    MinMapTileX: sectorBounds.MinMapTileX,
                    MinMapTileY: sectorBounds.MinMapTileY,
                    MaxMapTileX: sectorBounds.MaxMapTileX,
                    MaxMapTileY: sectorBounds.MaxMapTileY
                )
                : fallbackBounds;
            var revisionOverride =
                preservedSliceRevisionsByAssetPath is not null
                && preservedSliceRevisionsByAssetPath.TryGetValue(
                    sliceBuilder.SectorAssetPath,
                    out var preservedRevision
                )
                    ? preservedRevision
                    : (long?)null;
            slices[sliceIndex] = sliceBuilder.Build(bounds, revisionOverride);
        }

        var isTerrainMaterializationPartial =
            request.MaterializedTerrainSectorAssetPaths is not null
            && materializedTerrainSectorCount < totalTerrainSectorCount;
        EditorMapSectorScenePreview[] virtualTerrainSectors = isTerrainMaterializationPartial
            ? sourceSectors.ToArray()
            : [];
        var materializedTerrainSectorAssetPaths = request.MaterializedTerrainSectorAssetPaths is not null
            ? new HashSet<string>(request.MaterializedTerrainSectorAssetPaths, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sceneRevision = ComputeSceneRevision(mapName, request, slices, maxRight - minLeft, maxBottom - minTop);
        return new EditorMapFloorRenderPreview
        {
            MapName = mapName,
            ViewMode = request.ViewMode,
            TileWidthPixels = request.TileWidthPixels,
            TileHeightPixels = request.TileHeightPixels,
            WidthPixels = maxRight - minLeft,
            HeightPixels = maxBottom - minTop,
            SceneRevision = sceneRevision,
            Slices = slices,
            VirtualTerrainSectors = virtualTerrainSectors,
            MaterializedTerrainSectorAssetPaths = materializedTerrainSectorAssetPaths,
            TileOrderMap = tileOrderMap,
            ObjectOrderMap = objectOrderMap,
            ObjectAuxiliaryOrderMap = objectAuxiliaryOrderMap,
            OverlayOrderMap = overlayOrderMap,
            LightOrderMap = lightOrderMap,
            RoofOrderMap = roofOrderMap,
            RenderQueueOrderMap = renderQueueOrderMap,
            IncludeEditorObjectStateTint = request.IncludeEditorObjectStateTint,
            IncludeFloorLightTint = request.IncludeFloorLightTint,
            IncludeEmptyTerrainTiles = request.IncludeEmptyTiles,
            IncludeTerrainRoofs = request.IncludeRoofs,
            IncludeTerrainBlockedTileOverlays = request.IncludeBlockedTileOverlays,
            IncludeTerrainLightOverlays = request.IncludeLightOverlays,
            IncludeTerrainScriptOverlays = request.IncludeScriptOverlays,
            IncludeTerrainJumpPointOverlays = request.IncludeJumpPointOverlays,
            AmbientLighting = request.AmbientLighting,
            IsTerrainMaterializationPartial = isTerrainMaterializationPartial,
            MaterializedTerrainSectorCount = materializedTerrainSectorCount,
            TotalTerrainSectorCount = totalTerrainSectorCount,
            OffsetX = offsetX,
            OffsetY = offsetY,
            RawMinLeft = minLeft,
            RawMinTop = minTop,
            RawMaxRight = maxRight,
            RawMaxBottom = maxBottom,
        };
    }

    private static void UpdateSectorMapTileBounds(
        SectorAccumulator accumulator,
        int minMapTileX,
        int minMapTileY,
        int? maxMapTileX = null,
        int? maxMapTileY = null
    )
    {
        var resolvedMaxMapTileX = maxMapTileX ?? minMapTileX;
        var resolvedMaxMapTileY = maxMapTileY ?? minMapTileY;
        accumulator.MinMapTileX = Math.Min(accumulator.MinMapTileX, minMapTileX);
        accumulator.MinMapTileY = Math.Min(accumulator.MinMapTileY, minMapTileY);
        accumulator.MaxMapTileX = Math.Max(accumulator.MaxMapTileX, resolvedMaxMapTileX);
        accumulator.MaxMapTileY = Math.Max(accumulator.MaxMapTileY, resolvedMaxMapTileY);
    }

    private static EditorMapSectorRenderSliceBounds CreateSectorBounds(SectorAccumulator accumulator)
    {
        if (
            !accumulator.HasContent
            || double.IsInfinity(accumulator.MinLeft)
            || accumulator.MinMapTileX == int.MaxValue
        )
        {
            return new EditorMapSectorRenderSliceBounds(0d, 0d, 0d, 0d, 0, 0, 0, 0);
        }

        return new EditorMapSectorRenderSliceBounds(
            Left: accumulator.MinLeft,
            Top: accumulator.MinTop,
            Width: Math.Max(0d, accumulator.MaxRight - accumulator.MinLeft),
            Height: Math.Max(0d, accumulator.MaxBottom - accumulator.MinTop),
            MinMapTileX: accumulator.MinMapTileX,
            MinMapTileY: accumulator.MinMapTileY,
            MaxMapTileX: accumulator.MaxMapTileX,
            MaxMapTileY: accumulator.MaxMapTileY
        );
    }

    private static int GetTileIndex(int tileX, int tileY) => checked((tileY * 64) + tileX);

    internal static (double AnchorX, double AnchorY) ProjectObjectAnchor(
        EditorMapSceneViewMode viewMode,
        double tileWidthPixels,
        double tileHeightPixels,
        double tileCenterX,
        double tileCenterY,
        EditorMapObjectPreview objectPreview
    )
    {
        var (offsetX, offsetY, offsetZ) = ScaleObjectOffsets(
            viewMode,
            tileWidthPixels,
            tileHeightPixels,
            objectPreview
        );
        return (tileCenterX + offsetX, tileCenterY + offsetY - offsetZ);
    }

    private static (double OffsetX, double OffsetY, double OffsetZ) ScaleObjectOffsets(
        EditorMapSceneViewMode viewMode,
        double tileWidthPixels,
        double tileHeightPixels,
        EditorMapObjectPreview objectPreview
    )
    {
        if (viewMode is not EditorMapSceneViewMode.Isometric)
            return (objectPreview.OffsetX, objectPreview.OffsetY, objectPreview.OffsetZ);

        var scaleX = tileWidthPixels / 80d;
        var scaleY = tileHeightPixels / 40d;

        // CE object anchor: base_x = loc_x + offset_x + 40; rect.x = base_x - hot_x
        // Our tileCenterX = 40*(Y-X) = loc_x + 40, so no additional offset is needed.
        // Previously a -40/-20 adjustment was applied for Scenery type which was incorrect.
        var baseOffsetX = (double)objectPreview.OffsetX;
        var baseOffsetY = (double)objectPreview.OffsetY;

        return (baseOffsetX * scaleX, baseOffsetY * scaleY, objectPreview.OffsetZ * scaleY);
    }

    internal static long GetDrawOrder(EditorMapSceneViewMode viewMode, int mapTileWidth, int mapTileX, int mapTileY) =>
        viewMode switch
        {
            EditorMapSceneViewMode.TopDown => checked((((long)mapTileY * 10_000_000L) + mapTileX)),
            EditorMapSceneViewMode.Isometric => checked((((long)mapTileY + mapTileX) * 10_000_000L) + mapTileY),
            _ => throw new ArgumentOutOfRangeException(nameof(viewMode), viewMode, "Unsupported scene view mode."),
        };

    internal static (double CenterX, double CenterY) ProjectTileCenter(
        EditorMapSceneViewMode viewMode,
        double tileWidthPixels,
        double tileHeightPixels,
        int mapTileX,
        int mapTileY
    )
    {
        return viewMode switch
        {
            EditorMapSceneViewMode.TopDown => (
                (-mapTileX * tileWidthPixels) + (tileWidthPixels / 2d),
                (mapTileY * tileHeightPixels) + (tileHeightPixels / 2d)
            ),
            EditorMapSceneViewMode.Isometric => (
                (mapTileY - mapTileX) * (tileWidthPixels / 2d),
                ((mapTileX + mapTileY) * (tileHeightPixels / 2d)) + (tileHeightPixels / 2d)
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(viewMode), viewMode, "Unsupported scene view mode."),
        };
    }

    internal static double GetObjectSortKey(long baseTileDrawOrder, EditorMapObjectPreview objectPreview)
    {
        var (tileOrderPrimary, _) = GetObjectTileOrderComponents(objectPreview);
        return GetObjectSortKey(baseTileDrawOrder, tileOrderPrimary);
    }

    internal static (int Primary, int Secondary) GetObjectTileOrderComponents(EditorMapObjectPreview objectPreview)
    {
        var (offsetX, offsetY) = GetObjectTileOrderOffsets(objectPreview);
        var horizontal = (offsetX - 40) / 2;
        var vertical = 2 * (offsetY / 2);
        return (Primary: horizontal + vertical, Secondary: vertical - horizontal);
    }

    internal static double GetObjectTieBreakerSortKey(EditorMapObjectPreview objectPreview) =>
        (objectPreview.SpriteBounds?.MaxFrameCenterY ?? 0)
        + ((objectPreview.SpriteBounds?.MaxFrameHeight ?? 0) / 4096d)
        + (objectPreview.CollisionHeight / 16777216d);

    private static double GetObjectSortKey(long baseTileDrawOrder, int tileOrderPrimary) =>
        (baseTileDrawOrder * 4096d) + 2048d + tileOrderPrimary;

    private static (int OffsetX, int OffsetY) GetObjectTileOrderOffsets(EditorMapObjectPreview objectPreview) =>
        UsesCeWallPortalOrdering(objectPreview.ObjectType)
            ? (0, GetCeWallPortalOrderingOffsetY(objectPreview.CurrentArtId))
            : (objectPreview.OffsetX, objectPreview.OffsetY);

    private static bool UsesCeWallPortalOrdering(ObjectType objectType) =>
        objectType is ObjectType.Wall or ObjectType.Portal;

    private static int GetCeWallPortalOrderingOffsetY(ArtId artId)
    {
        var rotationIndex = (int)((artId.Value >> 11) & 0x7u);
        return rotationIndex is > 1 and < 6 ? 19 : -20;
    }

    private static int GetObjectTypeSortPriority(ObjectType objectType) => objectType is ObjectType.Portal ? 1 : 0;

    internal static double GetTileOverlaySortKey(long tileDrawOrder, EditorMapTileOverlayKind kind) =>
        (tileDrawOrder * 4096d) + 1024d + (int)kind;

    internal static double GetRoofSortKey(long baseTileDrawOrder) => (baseTileDrawOrder * 4096d) + 3072d;

    internal static double GetTileOverlaySuggestedOpacity(EditorMapTileOverlayKind kind) =>
        kind switch
        {
            EditorMapTileOverlayKind.BlockedTile => 0.45d,
            EditorMapTileOverlayKind.Light => 0.4d,
            EditorMapTileOverlayKind.Script => 0.45d,
            EditorMapTileOverlayKind.JumpPoint => 0.45d,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported tile overlay kind."),
        };

    internal static uint GetTileOverlaySuggestedTintColor(EditorMapTileOverlayKind kind) =>
        kind switch
        {
            EditorMapTileOverlayKind.BlockedTile => 0x88CC6666u,
            EditorMapTileOverlayKind.Light => 0x88E0C85Au,
            EditorMapTileOverlayKind.Script => 0x88996CCCu,
            EditorMapTileOverlayKind.JumpPoint => 0x8866BBDDu,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported tile overlay kind."),
        };

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

    internal static void ExpandObjectBounds(
        double scaleX,
        double scaleY,
        EditorMapObjectPreview objectPreview,
        double anchorX,
        double anchorY,
        ref double minLeft,
        ref double minTop,
        ref double maxRight,
        ref double maxBottom
    )
    {
        var spriteBounds = objectPreview.SpriteBounds;
        if (spriteBounds is null)
        {
            minLeft = Math.Min(minLeft, anchorX);
            minTop = Math.Min(minTop, anchorY);
            maxRight = Math.Max(maxRight, anchorX);
            maxBottom = Math.Max(maxBottom, anchorY);
            return;
        }

        var (centerX, centerY) = GetLayoutSpriteCenter(objectPreview, spriteBounds);
        var left = anchorX - (centerX * scaleX);
        var top = anchorY - (centerY * scaleY);
        var right = left + (spriteBounds.MaxFrameWidth * scaleX);
        var bottom = top + (spriteBounds.MaxFrameHeight * scaleY);

        minLeft = Math.Min(minLeft, left);
        minTop = Math.Min(minTop, top);
        maxRight = Math.Max(maxRight, right);
        maxBottom = Math.Max(maxBottom, bottom);
    }

    internal static void ExpandObjectBounds(
        double scaleX,
        double scaleY,
        EditorMapPlacementPreviewObject previewObject,
        double anchorX,
        double anchorY,
        ref double minLeft,
        ref double minTop,
        ref double maxRight,
        ref double maxBottom
    )
    {
        ArgumentNullException.ThrowIfNull(previewObject);

        var spriteBounds = previewObject.SpriteBounds;
        if (spriteBounds is null)
        {
            minLeft = Math.Min(minLeft, anchorX);
            minTop = Math.Min(minTop, anchorY);
            maxRight = Math.Max(maxRight, anchorX);
            maxBottom = Math.Max(maxBottom, anchorY);
            return;
        }

        var (centerX, centerY) = GetLayoutSpriteCenter(
            previewObject.ObjectType,
            previewObject.CurrentArtId,
            spriteBounds
        );
        var left = anchorX - (centerX * scaleX);
        var top = anchorY - (centerY * scaleY);
        var right = left + (spriteBounds.MaxFrameWidth * scaleX);
        var bottom = top + (spriteBounds.MaxFrameHeight * scaleY);

        minLeft = Math.Min(minLeft, left);
        minTop = Math.Min(minTop, top);
        maxRight = Math.Max(maxRight, right);
        maxBottom = Math.Max(maxBottom, bottom);
    }

    public static (int CenterX, int CenterY) GetLayoutSpriteCenter(
        EditorMapObjectPreview objectPreview,
        EditorMapObjectSpriteBounds spriteBounds
    ) => GetLayoutSpriteCenter(objectPreview.ObjectType, objectPreview.CurrentArtId, spriteBounds);

    public static (int CenterX, int CenterY) GetLayoutSpriteCenter(
        ObjectType objectType,
        ArtId artId,
        EditorMapObjectSpriteBounds spriteBounds
    )
    {
        if (objectType is not ObjectType.Wall and not ObjectType.Portal)
            return (spriteBounds.MaxFrameCenterX, spriteBounds.MaxFrameCenterY);

        var rotationIndex = NormalizeWallPortalRotationIndex((int)((artId.Value >> 11) & 0x7u));
        var adjustedCenterX = spriteBounds.MaxFrameCenterX;
        var adjustedCenterY = spriteBounds.MaxFrameCenterY;

        // CE tig_art_frame_data applies an extra hotspot shift for the north/south-facing wall and portal
        // families before any mirror flip is evaluated.
        if (rotationIndex is < 2 or > 5)
        {
            adjustedCenterX -= 40;
            adjustedCenterY += 20;
        }

        if ((artId.Value & 0x1u) != 0)
            adjustedCenterX = spriteBounds.MaxFrameWidth - adjustedCenterX - 2;

        return (adjustedCenterX, adjustedCenterY);
    }

    private static int NormalizeWallPortalRotationIndex(int rotationIndex)
    {
        var normalizedRotationIndex = rotationIndex % 8;
        return normalizedRotationIndex < 0 ? normalizedRotationIndex + 8 : normalizedRotationIndex;
    }

    internal static (double AnchorX, double AnchorY) ProjectRoofAnchor(
        EditorMapSceneViewMode viewMode,
        double tileWidthPixels,
        double tileHeightPixels,
        int mapTileX,
        int topMapTileY
    )
    {
        return viewMode switch
        {
            EditorMapSceneViewMode.TopDown => (-mapTileX * tileWidthPixels, topMapTileY * tileHeightPixels),
            EditorMapSceneViewMode.Isometric => ProjectIsometricRoofAnchor(
                tileWidthPixels,
                tileHeightPixels,
                mapTileX,
                topMapTileY
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(viewMode), viewMode, "Unsupported scene view mode."),
        };
    }

    private static (double AnchorX, double AnchorY) ProjectIsometricRoofAnchor(
        double tileWidthPixels,
        double tileHeightPixels,
        int mapTileX,
        int topMapTileY
    )
    {
        var normalizedMapTileX = checked(mapTileX + 2);
        var normalizedMapTileY = checked(topMapTileY + 2);
        var (centerX, centerY) = ProjectTileCenter(
            EditorMapSceneViewMode.Isometric,
            tileWidthPixels,
            tileHeightPixels,
            normalizedMapTileX,
            normalizedMapTileY
        );
        return (centerX - (tileWidthPixels * 2d), centerY - (tileHeightPixels * 5.5d));
    }

    internal static void ExpandRoofBounds(
        EditorMapSceneViewMode viewMode,
        double tileWidthPixels,
        double tileHeightPixels,
        double anchorX,
        double anchorY,
        ref double minLeft,
        ref double minTop,
        ref double maxRight,
        ref double maxBottom
    )
    {
        if (viewMode is EditorMapSceneViewMode.TopDown)
        {
            minLeft = Math.Min(minLeft, anchorX);
            minTop = Math.Min(minTop, anchorY);
            maxRight = Math.Max(maxRight, anchorX + (tileWidthPixels * 4d));
            maxBottom = Math.Max(maxBottom, anchorY + (tileHeightPixels * 4d));
            return;
        }

        minLeft = Math.Min(minLeft, anchorX - (tileWidthPixels * 2d));
        minTop = Math.Min(minTop, anchorY - tileHeightPixels);
        maxRight = Math.Max(maxRight, anchorX + (tileWidthPixels * 2d));
        maxBottom = Math.Max(maxBottom, anchorY + (tileHeightPixels * 2d));
    }

    private static uint[] BuildRenderQueue(
        IReadOnlyList<RawTileRenderItem> rawTiles,
        IReadOnlyList<uint> tileOrderMap,
        IReadOnlyList<RawTileOverlayRenderItem> rawTileOverlays,
        IReadOnlyList<uint> overlayOrderMap,
        IReadOnlyList<RawObjectRenderItem> rawObjects,
        IReadOnlyList<uint> objectOrderMap,
        IReadOnlyList<RawRoofRenderItem> rawRoofs,
        IReadOnlyList<uint> roofOrderMap,
        IReadOnlyList<RawAuxiliaryRenderItem> rawAuxiliaries,
        IReadOnlyList<uint> objectAuxiliaryOrderMap,
        IReadOnlyList<EditorMapLightRenderItem> lights,
        IReadOnlyList<uint> lightOrderMap,
        IReadOnlyList<SectorRenderSliceBuilder> sliceBuilders
    )
    {
        var renderQueueOrderMap = new uint[
            rawTiles.Count
                + rawTileOverlays.Count
                + rawObjects.Count
                + rawRoofs.Count
                + rawAuxiliaries.Count
                + lights.Count
        ];
        for (var i = 0; i < rawTiles.Count; i++)
        {
            var packedPayloadIndex = tileOrderMap[i];
            var (sliceIndex, payloadIndex) = EditorMapFloorRenderPreview.UnpackSliceItemIndex(packedPayloadIndex);
            var sliceQueue = sliceBuilders[sliceIndex].Queue;
            renderQueueOrderMap[i] = EditorMapFloorRenderPreview.PackSliceItemIndex(sliceIndex, sliceQueue.Count);
            sliceQueue.Add(
                new EditorMapRenderIndexEntry(
                    EditorMapRenderQueueItemKind.FloorTile,
                    payloadIndex,
                    -2_000_000_000d + i,
                    i
                )
            );
        }

        List<RenderQueueSortItem> queue = new(
            rawTileOverlays.Count + rawObjects.Count + rawRoofs.Count + rawAuxiliaries.Count + lights.Count
        );

        for (var i = 0; i < rawTileOverlays.Count; i++)
            queue.Add(
                new RenderQueueSortItem(
                    -1_000_000_000d + (i * 4d) + (int)rawTileOverlays[i].Kind,
                    EditorMapRenderQueueItemKind.TileOverlay,
                    i
                )
            );

        var underlayCounter = 0d;
        for (var index = 0; index < rawAuxiliaries.Count; index++)
        {
            if (rawAuxiliaries[index].Layer is not EditorMapObjectAuxiliaryRenderLayer.Underlay)
                continue;

            queue.Add(
                new RenderQueueSortItem(0d + underlayCounter++, EditorMapRenderQueueItemKind.ObjectAuxiliary, index)
            );
        }

        var underAllCounter = 0d;
        for (var index = 0; index < rawObjects.Count; index++)
        {
            if (!IsUnderAllScenery(rawObjects[index]))
                continue;

            queue.Add(
                new RenderQueueSortItem(100_000_000d + underAllCounter++, EditorMapRenderQueueItemKind.Object, index)
            );
        }

        var flatCounter = 0d;
        for (var index = 0; index < rawObjects.Count; index++)
        {
            if (!IsFlatObject(rawObjects[index]) || IsUnderAllScenery(rawObjects[index]))
                continue;

            queue.Add(
                new RenderQueueSortItem(200_000_000d + flatCounter++, EditorMapRenderQueueItemKind.Object, index)
            );
        }

        var shadowCounter = 0d;
        for (var index = 0; index < rawAuxiliaries.Count; index++)
        {
            if (rawAuxiliaries[index].Layer is not EditorMapObjectAuxiliaryRenderLayer.Shadow)
                continue;

            queue.Add(
                new RenderQueueSortItem(
                    400_000_000d + shadowCounter++,
                    EditorMapRenderQueueItemKind.ObjectAuxiliary,
                    index
                )
            );
        }

        var nonFlatList = new List<NonFlatSortItem>();

        for (var index = 0; index < rawObjects.Count; index++)
        {
            if (IsFlatObject(rawObjects[index]))
                continue;

            var obj = rawObjects[index];
            nonFlatList.Add(
                new NonFlatSortItem(
                    Kind: EditorMapRenderQueueItemKind.Object,
                    Index: index,
                    BaseTileDrawOrder: obj.BaseTileDrawOrder,
                    SameTileOrder: obj.SameTileOrder,
                    MapTileX: obj.MapTileX,
                    MapTileY: obj.MapTileY,
                    SubOrder: 0,
                    SlotOrder: 0
                )
            );
        }

        for (var index = 0; index < rawAuxiliaries.Count; index++)
        {
            var aux = rawAuxiliaries[index];
            if (!IsGhostOrArmorOverlay(aux))
                continue;

            nonFlatList.Add(
                new NonFlatSortItem(
                    Kind: EditorMapRenderQueueItemKind.ObjectAuxiliary,
                    Index: index,
                    BaseTileDrawOrder: aux.ParentBaseTileDrawOrder,
                    SameTileOrder: aux.ParentSameTileOrder,
                    MapTileX: aux.MapTileX,
                    MapTileY: aux.MapTileY,
                    SubOrder: 1,
                    SlotOrder: aux.SlotOrder
                )
            );
        }

        nonFlatList.Sort(
            (a, b) =>
            {
                var cmp = a.BaseTileDrawOrder.CompareTo(b.BaseTileDrawOrder);
                if (cmp != 0)
                    return cmp;

                cmp = a.SameTileOrder.CompareTo(b.SameTileOrder);
                if (cmp != 0)
                    return cmp;

                cmp = a.MapTileX.CompareTo(b.MapTileX);
                if (cmp != 0)
                    return cmp;

                cmp = a.MapTileY.CompareTo(b.MapTileY);
                if (cmp != 0)
                    return cmp;

                cmp = a.SubOrder.CompareTo(b.SubOrder);
                if (cmp != 0)
                    return cmp;

                return a.SlotOrder.CompareTo(b.SlotOrder);
            }
        );

        var nonFlatCounter = 0d;
        foreach (var item in nonFlatList)
            queue.Add(new RenderQueueSortItem(600_000_000d + nonFlatCounter++, item.Kind, item.Index));

        var overlayCounter = 0d;
        for (var index = 0; index < rawAuxiliaries.Count; index++)
        {
            var aux = rawAuxiliaries[index];
            if (
                aux.Layer
                is not (
                    EditorMapObjectAuxiliaryRenderLayer.OverlayBack
                    or EditorMapObjectAuxiliaryRenderLayer.OverlayFore
                )
            )
            {
                continue;
            }

            if (IsGhostOrArmorOverlay(aux))
                continue;

            queue.Add(
                new RenderQueueSortItem(
                    700_000_000d + overlayCounter++,
                    EditorMapRenderQueueItemKind.ObjectAuxiliary,
                    index
                )
            );
        }

        var roofCounter = 0d;
        for (var index = 0; index < rawRoofs.Count; index++)
            queue.Add(new RenderQueueSortItem(800_000_000d + roofCounter++, EditorMapRenderQueueItemKind.Roof, index));

        var lightCounter = 0d;
        for (var index = 0; index < lights.Count; index++)
            queue.Add(
                new RenderQueueSortItem(900_000_000d + lightCounter++, EditorMapRenderQueueItemKind.Light, index)
            );

        queue.Sort(
            static (a, b) =>
            {
                var cmp = a.SortKey.CompareTo(b.SortKey);
                if (cmp != 0)
                    return cmp;

                cmp = a.Kind.CompareTo(b.Kind);
                return cmp != 0 ? cmp : a.Index.CompareTo(b.Index);
            }
        );

        for (var queueIndex = 0; queueIndex < queue.Count; queueIndex++)
        {
            var drawOrder = rawTiles.Count + queueIndex;
            var item = queue[queueIndex];
            var packedPayloadIndex = item.Kind switch
            {
                EditorMapRenderQueueItemKind.Object => objectOrderMap[item.Index],
                EditorMapRenderQueueItemKind.TileOverlay => overlayOrderMap[item.Index],
                EditorMapRenderQueueItemKind.Roof => roofOrderMap[item.Index],
                EditorMapRenderQueueItemKind.ObjectAuxiliary => objectAuxiliaryOrderMap[item.Index],
                EditorMapRenderQueueItemKind.Light => lightOrderMap[item.Index],
                _ => throw new ArgumentOutOfRangeException(
                    nameof(item.Kind),
                    item.Kind,
                    "Unsupported render queue kind."
                ),
            };

            var (sliceIndex, payloadIndex) = EditorMapFloorRenderPreview.UnpackSliceItemIndex(packedPayloadIndex);
            var sliceQueue = sliceBuilders[sliceIndex].Queue;
            renderQueueOrderMap[drawOrder] = EditorMapFloorRenderPreview.PackSliceItemIndex(
                sliceIndex,
                sliceQueue.Count
            );
            sliceQueue.Add(new EditorMapRenderIndexEntry(item.Kind, payloadIndex, item.SortKey, drawOrder));
        }

        return renderQueueOrderMap;
    }

    private static void ValidateRequest(EditorMapFloorRenderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!double.IsFinite(request.TileWidthPixels) || request.TileWidthPixels <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.TileWidthPixels,
                "Tile width must be a finite positive value."
            );
        }

        if (!double.IsFinite(request.TileHeightPixels) || request.TileHeightPixels <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.TileHeightPixels,
                "Tile height must be a finite positive value."
            );
        }
    }
}
