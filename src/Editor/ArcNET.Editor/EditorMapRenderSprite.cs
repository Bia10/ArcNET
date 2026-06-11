using ArcNET.Core.Primitives;
using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Request used by host-facing map sprite sources when resolving one paintable ART frame.
/// </summary>
public sealed class EditorMapRenderSpriteRequest
{
    /// <summary>
    /// Optional render-queue kind that is requesting the sprite.
    /// Hosts can use this to disambiguate low Arcanum terrain art ids whose backing asset category
    /// depends on whether the item is a floor tile or a roof tile.
    /// </summary>
    public EditorMapRenderQueueItemKind? RenderItemKind { get; init; }

    /// <summary>
    /// Zero-based ART rotation index to resolve.
    /// </summary>
    public int RotationIndex { get; init; }

    /// <summary>
    /// Zero-based frame index within <see cref="RotationIndex"/>.
    /// </summary>
    public int FrameIndex { get; init; }

    /// <summary>
    /// Optional CE scale percentage hint used by some hosts for cache keys and variant resolution.
    /// </summary>
    public int ScalePercent { get; init; } = 100;

    /// <summary>
    /// Indicates whether one CE shrunk variant is requested when available.
    /// </summary>
    public bool IsShrunk { get; init; }
}

/// <summary>
/// Host-facing paintable ART frame resolved for one map render item.
/// </summary>
public sealed class EditorMapRenderSprite
{
    public required ArtId ArtId { get; init; }
    public required string AssetPath { get; init; }
    public EditorMapRenderQueueItemKind? RenderItemKind { get; init; }
    public required int RotationIndex { get; init; }
    public required int FrameIndex { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int Stride { get; init; }
    public required int CenterX { get; init; }
    public required int CenterY { get; init; }
    public int DeltaX { get; init; }
    public int DeltaY { get; init; }
    public required uint FrameRate { get; init; }
    public int FramesPerRotation { get; init; } = 1;
    public required EditorArtPreviewPixelFormat PixelFormat { get; init; }
    public required byte[] PixelData { get; init; }
}

/// <summary>
/// Lightweight sprite-frame metrics that can be used for layout and viewport indexing without decoding frame pixels.
/// </summary>
public sealed class EditorMapRenderSpriteMetrics
{
    public string? AssetPath { get; init; }
    public int RotationIndex { get; init; }
    public int FrameIndex { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int CenterX { get; init; }
    public required int CenterY { get; init; }
    public int DeltaX { get; init; }
    public int DeltaY { get; init; }
    public int FramesPerRotation { get; init; } = 1;
    public uint FrameRate { get; init; }

    public static EditorMapRenderSpriteMetrics FromSprite(EditorMapRenderSprite sprite)
    {
        ArgumentNullException.ThrowIfNull(sprite);

        return new EditorMapRenderSpriteMetrics
        {
            AssetPath = sprite.AssetPath,
            RotationIndex = sprite.RotationIndex,
            FrameIndex = sprite.FrameIndex,
            Width = sprite.Width,
            Height = sprite.Height,
            CenterX = sprite.CenterX,
            CenterY = sprite.CenterY,
            DeltaX = sprite.DeltaX,
            DeltaY = sprite.DeltaY,
            FrameRate = sprite.FrameRate,
            FramesPerRotation = sprite.FramesPerRotation,
        };
    }

    internal static EditorMapRenderSpriteMetrics ApplyCeScale(
        EditorMapRenderSpriteMetrics metrics,
        int scalePercent,
        bool isShrunk
    )
    {
        ArgumentNullException.ThrowIfNull(metrics);

        if (scalePercent == 100 && !isShrunk)
            return metrics;

        var width = metrics.Width;
        var height = metrics.Height;
        var centerX = metrics.CenterX;
        var centerY = metrics.CenterY;
        var deltaX = metrics.DeltaX;
        var deltaY = metrics.DeltaY;

        if (scalePercent != 100)
        {
            width = (int)((float)width * scalePercent / 100f);
            height = (int)((float)height * scalePercent / 100f);
            centerX = (int)((float)centerX * scalePercent / 100f);
            centerY = (int)((float)centerY * scalePercent / 100f);
            deltaX = (int)((float)deltaX * scalePercent / 100f);
            deltaY = (int)((float)deltaY * scalePercent / 100f);
        }

        if (isShrunk)
        {
            width /= 2;
            height /= 2;
            centerX /= 2;
            centerY /= 2;
            deltaX /= 2;
            deltaY /= 2;
        }

        return new EditorMapRenderSpriteMetrics
        {
            AssetPath = metrics.AssetPath,
            RotationIndex = metrics.RotationIndex,
            FrameIndex = metrics.FrameIndex,
            Width = width,
            Height = height,
            CenterX = centerX,
            CenterY = centerY,
            DeltaX = deltaX,
            DeltaY = deltaY,
            FrameRate = metrics.FrameRate,
            FramesPerRotation = metrics.FramesPerRotation,
        };
    }
}

/// <summary>
/// Host-provided or workspace-backed ART frame resolver used when turning one normalized scene queue into paintable items.
/// </summary>
public interface IEditorMapRenderSpriteSource
{
    /// <summary>
    /// Resolves one paintable ART frame for <paramref name="artId"/>, or returns <see langword="null"/> when no binding exists.
    /// </summary>
    EditorMapRenderSprite? Resolve(ArtId artId, EditorMapRenderSpriteRequest? request = null);

    /// <summary>
    /// Resolves lightweight frame metrics for <paramref name="artId"/> without requiring full pixel materialization when the source supports a cheaper path.
    /// </summary>
    EditorMapRenderSpriteMetrics? GetSpriteMetrics(ArtId artId, EditorMapRenderSpriteRequest? request = null) =>
        Resolve(artId, request) is { } sprite ? EditorMapRenderSpriteMetrics.FromSprite(sprite) : null;

    /// <summary>
    /// Checks whether <paramref name="artId"/> can resolve without forcing full frame materialization when the source supports a cheaper probe.
    /// </summary>
    bool CanResolve(ArtId artId, EditorMapRenderSpriteRequest? request = null) => Resolve(artId, request) is not null;

    /// <summary>
    /// Asynchronously preloads required sprites into the cache to prevent thread pool starvation during parallel queries.
    /// </summary>
    Task PreloadAsync(IEnumerable<EditorMapRenderQueueItem> items, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <summary>
    /// Asynchronously preloads sprites referenced by a committed slice-backed scene without requiring the unified queue to expand.
    /// </summary>
    Task PreloadAsync(EditorMapFloorRenderPreview sceneRender, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sceneRender);
        return PreloadAsync(sceneRender.RenderQueue, cancellationToken);
    }
}

/// <summary>
/// Workspace-backed sprite source that resolves bound <see cref="ArtId"/> values through one <see cref="EditorArtResolver"/>
/// and caches preview-ready frames for host rendering.
/// </summary>
public sealed class EditorWorkspaceMapRenderSpriteSource : IEditorMapRenderSpriteSource
{
    private const long DefaultMaxRetainedBytes = 64L * 1024L * 1024L;
    private const int DefaultMaxEntryCount = 1024;
    private const uint ArtTypeMask = 0xF0000000u;
    private const uint WallArtType = 0x10000000u;
    private const uint PortalArtType = 0x30000000u;
    private const uint SceneryArtType = 0x40000000u;
    private const int ArtIdRotationShift = 11;
    private const uint InterfaceArtType = 0x50000000u;
    private const uint ItemArtType = 0x60000000u;
    private const uint MiscArtType = 0x80000000u;
    private const uint LightArtType = 0x90000000u;
    private const uint FacadeArtType = 0xB0000000u;
    private const uint EyeCandyArtType = 0xE0000000u;
    private readonly EditorWorkspace _workspace;
    private readonly EditorArtResolver _artResolver;
    private readonly EditorArtPreviewOptions _previewOptions;
    private readonly object _cacheGate = new();
    private readonly Dictionary<EditorMapRenderSpriteAssetPathCacheKey, string?> _assetPathCache = [];
    private readonly Dictionary<EditorMapRenderSpriteCacheKey, EditorMapRenderSprite?> _cache = [];
    private readonly Dictionary<EditorMapRenderSpriteCacheKey, EditorMapRenderSpriteMetrics?> _metricsCache = [];
    private readonly RetainedCacheBudget<EditorMapRenderSpriteCacheKey> _budget;

    public EditorWorkspaceMapRenderSpriteSource(
        EditorWorkspace workspace,
        EditorArtResolver artResolver,
        EditorArtPreviewOptions? previewOptions = null
    )
        : this(workspace, artResolver, previewOptions, DefaultMaxRetainedBytes, DefaultMaxEntryCount) { }

    internal EditorWorkspaceMapRenderSpriteSource(
        EditorWorkspace workspace,
        EditorArtResolver artResolver,
        EditorArtPreviewOptions? previewOptions,
        long maxRetainedBytes,
        int maxEntryCount
    )
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(artResolver);

        _workspace = workspace;
        _artResolver = artResolver;
        _previewOptions = previewOptions ?? new EditorArtPreviewOptions();
        _budget = new(comparer: null, maxRetainedBytes, maxEntryCount);
    }

    /// <summary>
    /// Total number of cached ART frame lookups stored by this source.
    /// </summary>
    public int CachedFrameCount
    {
        get
        {
            lock (_cacheGate)
                return _cache.Count;
        }
    }

    public long CachedFrameRetainedBytes
    {
        get
        {
            lock (_cacheGate)
                return _budget.RetainedBytes;
        }
    }

    internal int CachedResolvedAssetPathCount
    {
        get
        {
            lock (_cacheGate)
                return _assetPathCache.Count;
        }
    }

    internal int CachedMetricsCount
    {
        get
        {
            lock (_cacheGate)
                return _metricsCache.Count;
        }
    }

    /// <inheritdoc />
    public EditorMapRenderSpriteMetrics? GetSpriteMetrics(ArtId artId, EditorMapRenderSpriteRequest? request = null)
    {
        if (artId.Value == 0)
            return null;

        var effectiveRequest = request ?? new EditorMapRenderSpriteRequest();
        var cacheKey = new EditorMapRenderSpriteCacheKey(
            artId,
            effectiveRequest.RenderItemKind,
            effectiveRequest.RotationIndex,
            effectiveRequest.FrameIndex
        );
        lock (_cacheGate)
        {
            if (_cache.TryGetValue(cacheKey, out var cachedSprite))
            {
                _budget.TryTouch(cacheKey);
                return cachedSprite is null
                    ? null
                    : EditorMapRenderSpriteMetrics.ApplyCeScale(
                        EditorMapRenderSpriteMetrics.FromSprite(cachedSprite),
                        effectiveRequest.ScalePercent,
                        effectiveRequest.IsShrunk
                    );
            }

            if (_metricsCache.TryGetValue(cacheKey, out var cachedMetrics))
            {
                return cachedMetrics is null
                    ? null
                    : EditorMapRenderSpriteMetrics.ApplyCeScale(
                        cachedMetrics,
                        effectiveRequest.ScalePercent,
                        effectiveRequest.IsShrunk
                    );
            }
        }

        var assetPath = TryResolveAssetPath(artId, effectiveRequest);
        if (assetPath is null)
            return null;

        var metrics = CreateResolvedSpriteMetrics(artId, effectiveRequest, assetPath);
        lock (_cacheGate)
        {
            if (!_metricsCache.ContainsKey(cacheKey))
                _metricsCache[cacheKey] = metrics;
        }

        return metrics is null
            ? null
            : EditorMapRenderSpriteMetrics.ApplyCeScale(
                metrics,
                effectiveRequest.ScalePercent,
                effectiveRequest.IsShrunk
            );
    }

    /// <inheritdoc />
    public EditorMapRenderSprite? Resolve(ArtId artId, EditorMapRenderSpriteRequest? request = null)
    {
        if (artId.Value == 0)
            return null;

        var effectiveRequest = request ?? new EditorMapRenderSpriteRequest();
        var cacheKey = new EditorMapRenderSpriteCacheKey(
            artId,
            effectiveRequest.RenderItemKind,
            effectiveRequest.RotationIndex,
            effectiveRequest.FrameIndex
        );
        lock (_cacheGate)
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                _budget.TryTouch(cacheKey);
                return cached;
            }
        }

        var resolved = ResolveCore(artId, effectiveRequest);
        lock (_cacheGate)
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                _budget.TryTouch(cacheKey);
                return cached;
            }

            _cache[cacheKey] = resolved;
            _metricsCache[cacheKey] = resolved is null ? null : EditorMapRenderSpriteMetrics.FromSprite(resolved);
            Evict(_budget.Register(cacheKey, EstimateRetainedBytes(resolved)));
            return resolved;
        }
    }

    /// <inheritdoc />
    public bool CanResolve(ArtId artId, EditorMapRenderSpriteRequest? request = null)
    {
        if (artId.Value == 0)
            return false;

        var effectiveRequest = request ?? new EditorMapRenderSpriteRequest();
        var cacheKey = new EditorMapRenderSpriteCacheKey(
            artId,
            effectiveRequest.RenderItemKind,
            effectiveRequest.RotationIndex,
            effectiveRequest.FrameIndex
        );
        lock (_cacheGate)
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                _budget.TryTouch(cacheKey);
                return cached is not null;
            }
        }

        return TryResolveAssetPath(artId, effectiveRequest) is not null;
    }

    /// <inheritdoc />
    public Task PreloadAsync(IEnumerable<EditorMapRenderQueueItem> items, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        var assetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var artId = TryGetArtId(item);
            if (artId is not { Value: not 0u } resolvedArtId)
                continue;

            var request = new EditorMapRenderSpriteRequest { RenderItemKind = item.Kind };
            if (TryResolveAssetPath(resolvedArtId, request) is { } assetPath)
                assetPaths.Add(assetPath);
        }

        return _workspace.PreloadArtsAsync(assetPaths, cancellationToken);
    }

    /// <inheritdoc />
    public Task PreloadAsync(EditorMapFloorRenderPreview sceneRender, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sceneRender);

        var assetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var sliceIndex = 0; sliceIndex < sceneRender.Slices.Count; sliceIndex++)
        {
            var slice = sceneRender.Slices[sliceIndex];
            for (var i = 0; i < slice.Tiles.Count; i++)
                AddAssetPath(assetPaths, slice.Tiles[i].ArtId, EditorMapRenderQueueItemKind.FloorTile);

            for (var i = 0; i < slice.Objects.Count; i++)
                AddAssetPath(assetPaths, slice.Objects[i].CurrentArtId, EditorMapRenderQueueItemKind.Object);

            for (var i = 0; i < slice.ObjectAuxiliaryItems.Count; i++)
                AddAssetPath(
                    assetPaths,
                    slice.ObjectAuxiliaryItems[i].ArtId,
                    EditorMapRenderQueueItemKind.ObjectAuxiliary
                );

            for (var i = 0; i < slice.Roofs.Count; i++)
                AddAssetPath(assetPaths, slice.Roofs[i].ArtId, EditorMapRenderQueueItemKind.Roof);

            for (var i = 0; i < slice.Lights.Count; i++)
                AddAssetPath(assetPaths, slice.Lights[i].ArtId, EditorMapRenderQueueItemKind.Light);
        }

        for (var sectorIndex = 0; sectorIndex < sceneRender.VirtualTerrainSectors.Count; sectorIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sector = sceneRender.VirtualTerrainSectors[sectorIndex];
            if (sceneRender.IsTerrainSectorMaterialized(sector.AssetPath))
                continue;

            var floorArtIds = sector.UniqueTerrainFloorArtIds;
            for (var tileIndex = 0; tileIndex < floorArtIds.Count; tileIndex++)
                AddAssetPath(assetPaths, floorArtIds[tileIndex], EditorMapRenderQueueItemKind.FloorTile);

            if (sceneRender.IncludeTerrainRoofs)
            {
                var roofArtIds = sector.UniqueTerrainRoofArtIds;
                for (var roofIndex = 0; roofIndex < roofArtIds.Count; roofIndex++)
                    AddAssetPath(assetPaths, roofArtIds[roofIndex], EditorMapRenderQueueItemKind.Roof);
            }

            if (!sceneRender.IncludeTerrainLightOverlays)
                continue;

            var lightArtIds = sector.UniqueTerrainLightArtIds;
            for (var lightIndex = 0; lightIndex < lightArtIds.Count; lightIndex++)
                AddAssetPath(assetPaths, lightArtIds[lightIndex], EditorMapRenderQueueItemKind.Light);
        }

        return _workspace.PreloadArtsAsync(assetPaths, cancellationToken);
    }

    private void AddAssetPath(HashSet<string> assetPaths, ArtId artId, EditorMapRenderQueueItemKind renderItemKind)
    {
        if (artId.Value == 0)
            return;

        var request = new EditorMapRenderSpriteRequest { RenderItemKind = renderItemKind };
        if (TryResolveAssetPath(artId, request) is { } assetPath)
            assetPaths.Add(assetPath);
    }

    private static ArtId? TryGetArtId(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.FloorTile => item.Tile?.ArtId,
            EditorMapRenderQueueItemKind.Object => item.Object?.CurrentArtId,
            EditorMapRenderQueueItemKind.ObjectAuxiliary => item.ObjectAuxiliaryItem?.ArtId,
            EditorMapRenderQueueItemKind.Roof => item.Roof?.ArtId,
            EditorMapRenderQueueItemKind.Light => item.Light?.ArtId,
            EditorMapRenderQueueItemKind.PlacementPreviewObject => item.PlacementPreviewObject?.CurrentArtId,
            _ => null,
        };

    private EditorMapRenderSprite? ResolveCore(ArtId artId, EditorMapRenderSpriteRequest request)
    {
        var assetPath = TryResolveAssetPath(artId, request);
        if (assetPath is null)
            return null;

        return CreateResolvedSprite(artId, request, assetPath);
    }

    private string? TryResolveAssetPath(ArtId artId, EditorMapRenderSpriteRequest request)
    {
        var cacheKey = new EditorMapRenderSpriteAssetPathCacheKey(artId, request.RenderItemKind);
        lock (_cacheGate)
        {
            if (_assetPathCache.TryGetValue(cacheKey, out var cachedAssetPath))
                return cachedAssetPath;
        }

        var resolvedAssetPath = TryResolveAssetPathCore(artId, request);
        lock (_cacheGate)
        {
            if (_assetPathCache.TryGetValue(cacheKey, out var cachedAssetPath))
                return cachedAssetPath;

            _assetPathCache[cacheKey] = resolvedAssetPath;
            return resolvedAssetPath;
        }
    }

    private string? TryResolveAssetPathCore(ArtId artId, EditorMapRenderSpriteRequest request)
    {
        if (IsSectorArtId(artId.Value) && request.RenderItemKind is EditorMapRenderQueueItemKind.FloorTile)
        {
            if (_workspace.TryResolveMapRenderArtAssetPath(artId, request.RenderItemKind, out var floorAssetPath))
                return floorAssetPath;

            var lowSectorFallbackAssetPath = _artResolver.FindAssetPath(artId);
            if (!string.IsNullOrWhiteSpace(lowSectorFallbackAssetPath))
            {
                var normPath = lowSectorFallbackAssetPath.Replace('\\', '/');
                if (IsSectorArtFamilyAssetPath(normPath) && !IsCompatibleFamily(request.RenderItemKind, normPath))
                    return null;
                return lowSectorFallbackAssetPath;
            }
            return null;
        }

        var assetPath = _artResolver.FindAssetPath(artId);
        if (
            string.IsNullOrWhiteSpace(assetPath)
            && _workspace.TryResolveMapRenderArtAssetPath(artId, request.RenderItemKind, out var hintedAssetPath)
        )
        {
            assetPath = hintedAssetPath;
        }

        if (string.IsNullOrWhiteSpace(assetPath))
            return null;

        var normalizedPath = assetPath.Replace('\\', '/');
        if (
            request.RenderItemKind is EditorMapRenderQueueItemKind.Roof
            && !normalizedPath.StartsWith("art/roof/", StringComparison.OrdinalIgnoreCase)
        )
        {
            return null;
        }

        if (
            IsSectorArtId(artId.Value)
            && IsSectorArtFamilyAssetPath(normalizedPath)
            && !IsCompatibleFamily(request.RenderItemKind, normalizedPath)
        )
        {
            return null;
        }

        return assetPath;
    }

    private static bool IsSectorArtId(uint artIdValue) => (artIdValue & 0xF0000000u) == 0u;

    private EditorMapRenderSprite? CreateResolvedSprite(
        ArtId artId,
        EditorMapRenderSpriteRequest request,
        string assetPath
    )
    {
        var art = _workspace.FindArt(assetPath);
        if (art is null || art.EffectiveRotationCount <= 0 || art.FrameCount == 0)
            return null;

        var effectiveRotationIndex = ResolveEffectiveRotationIndex(artId, request.RotationIndex);
        var rotationIndex = NormalizeFrameIndex(effectiveRotationIndex, art.EffectiveRotationCount);
        var effectiveFrameIndex = ResolveEffectiveFrameIndex(artId, request.FrameIndex);
        var frameIndex = NormalizeFrameIndex(effectiveFrameIndex, checked((int)art.FrameCount));
        var frame = EditorArtPreviewBuilder.BuildFrame(
            art,
            rotationIndex,
            frameIndex,
            ApplyArtIdPalette(_previewOptions, artId)
        );

        var (centerX, centerY) = AdjustSpriteCenter(
            request.RenderItemKind,
            assetPath,
            artId,
            effectiveRotationIndex,
            frame.Width,
            frame.Height,
            frame.Header.CenterX,
            frame.Header.CenterY
        );

        var sprite = new EditorMapRenderSprite
        {
            ArtId = artId,
            AssetPath = assetPath,
            RenderItemKind = request.RenderItemKind,
            RotationIndex = rotationIndex,
            FrameIndex = frameIndex,
            Width = frame.Width,
            Height = frame.Height,
            Stride = frame.Stride,
            CenterX = centerX,
            CenterY = centerY,
            DeltaX = frame.Header.DeltaX,
            DeltaY = frame.Header.DeltaY,
            FrameRate = art.FrameRate,
            FramesPerRotation = checked((int)art.FrameCount),
            PixelFormat = _previewOptions.PixelFormat,
            PixelData = frame.PixelData,
        };

        ApplyCeHorizontalTileMirror(request.RenderItemKind, artId, sprite.PixelData, sprite.Width, sprite.Height);

        return sprite;
    }

    internal static EditorArtPreviewOptions ApplyArtIdPalette(EditorArtPreviewOptions previewOptions, ArtId artId)
    {
        ArgumentNullException.ThrowIfNull(previewOptions);

        var isLight = artId.Type is ArtId.TypeCode.Light;
        // CE's tig_art_id_palette_get returns palette slot 0 for light and facade ART IDs.
        // art_anim_data.palette1 refers to the cache's modified version of the selected slot,
        // not a hard-coded "palette 1" ART file slot.
        var paletteSlot = artId.PaletteIndex;

        return new EditorArtPreviewOptions
        {
            PaletteSlot = paletteSlot,
            PixelFormat = previewOptions.PixelFormat,
            FlipVertically = previewOptions.FlipVertically,
            IsLightMask = isLight,
        };
    }

    private EditorMapRenderSpriteMetrics? CreateResolvedSpriteMetrics(
        ArtId artId,
        EditorMapRenderSpriteRequest request,
        string assetPath
    )
    {
        var art = TryGetResolvedArt(assetPath);
        if (art is null || art.EffectiveRotationCount <= 0 || art.FrameCount == 0)
            return null;

        var effectiveRotationIndex = ResolveEffectiveRotationIndex(artId, request.RotationIndex);
        var rotationIndex = NormalizeFrameIndex(effectiveRotationIndex, art.EffectiveRotationCount);
        var effectiveFrameIndex = ResolveEffectiveFrameIndex(artId, request.FrameIndex);
        var frameIndex = NormalizeFrameIndex(effectiveFrameIndex, checked((int)art.FrameCount));
        var header = art.Frames[rotationIndex][frameIndex].Header;
        var (centerX, centerY) = AdjustSpriteCenter(
            request.RenderItemKind,
            assetPath,
            artId,
            effectiveRotationIndex,
            checked((int)header.Width),
            checked((int)header.Height),
            header.CenterX,
            header.CenterY
        );

        return new EditorMapRenderSpriteMetrics
        {
            AssetPath = assetPath,
            RotationIndex = rotationIndex,
            FrameIndex = frameIndex,
            Width = checked((int)header.Width),
            Height = checked((int)header.Height),
            CenterX = centerX,
            CenterY = centerY,
            DeltaX = header.DeltaX,
            DeltaY = header.DeltaY,
            FrameRate = art.FrameRate,
            FramesPerRotation = checked((int)art.FrameCount),
        };
    }

    private static int ResolveEffectiveRotationIndex(ArtId artId, int requestedRotationIndex) =>
        UsesArtIdRotation(artId)
            ? NormalizeWallPortalRotationIndex((int)((artId.Value >> 11) & 0x7u))
            : requestedRotationIndex;

    private static int ResolveEffectiveFrameIndex(ArtId artId, int requestedFrameIndex) =>
        requestedFrameIndex != 0 ? requestedFrameIndex : artId.FrameIndex;

    internal static (int CenterX, int CenterY) AdjustSpriteCenter(
        EditorMapRenderQueueItemKind? renderItemKind,
        string assetPath,
        ArtId artId,
        int effectiveRotationIndex,
        int width,
        int height,
        int centerX,
        int centerY
    )
    {
        if (
            renderItemKind is EditorMapRenderQueueItemKind.Roof
            && artId.Type is ArtId.TypeCode.Roof
            && assetPath.StartsWith("art/roof/", StringComparison.OrdinalIgnoreCase)
            && artId.IsRoofMirrored
        )
        {
            return (0, centerY);
        }

        if (
            renderItemKind is EditorMapRenderQueueItemKind.FloorTile
            && artId.Type is ArtId.TypeCode.Tile or ArtId.TypeCode.Facade
            && !assetPath.StartsWith("art/wall/", StringComparison.OrdinalIgnoreCase)
            && !assetPath.StartsWith("art/portal/", StringComparison.OrdinalIgnoreCase)
        )
        {
            return ShouldApplyCeMirroredTileHotspot(renderItemKind, artId)
                ? ApplyCeMirroredHotspotOffsets(width, centerX, centerY)
                : (centerX, centerY);
        }

        if (
            !assetPath.StartsWith("art/wall/", StringComparison.OrdinalIgnoreCase)
            && !assetPath.StartsWith("art/portal/", StringComparison.OrdinalIgnoreCase)
        )
        {
            return (centerX, centerY);
        }

        if (renderItemKind is EditorMapRenderQueueItemKind.FloorTile)
            return (centerX, centerY);

        return artId.Type is ArtId.TypeCode.Wall or ArtId.TypeCode.Portal
            ? ApplyCeWallPortalHotspotOffsets(artId, width, centerX, centerY)
            : (centerX, centerY);
    }

    private static (int CenterX, int CenterY) AdjustSpriteCenter(
        string assetPath,
        int requestedRotationIndex,
        int centerX,
        int centerY
    ) => AdjustSpriteCenter(null, assetPath, default, requestedRotationIndex, width: 0, height: 0, centerX, centerY);

    private static bool UsesArtIdRotation(ArtId artId) =>
        (artId.Value & ArtTypeMask) is WallArtType or PortalArtType or SceneryArtType;

    private static int NormalizeWallPortalRotationIndex(int rotationIndex)
    {
        var normalizedRotationIndex = rotationIndex % 8;
        return normalizedRotationIndex < 0 ? normalizedRotationIndex + 8 : normalizedRotationIndex;
    }

    private static bool ShouldApplyCeMirroredTileHotspot(EditorMapRenderQueueItemKind? renderItemKind, ArtId artId) =>
        renderItemKind is EditorMapRenderQueueItemKind.FloorTile
        // CE only treats the low bit as a mirror flag for true tile AIDs. Facade AIDs use that
        // bit for walkability, so applying the tile mirror path there scrambles rug/stair pieces.
        && (artId.Value & 0x1u) != 0
        && artId.Type is ArtId.TypeCode.Tile;

    private static bool ShouldApplyCeHorizontalTileMirror(EditorMapRenderQueueItemKind? renderItemKind, ArtId artId) =>
        renderItemKind is EditorMapRenderQueueItemKind.FloorTile
        && ShouldApplyCeMirroredTileHotspot(renderItemKind, artId);

    internal static void ApplyCeHorizontalTileMirror(
        EditorMapRenderQueueItemKind? renderItemKind,
        ArtId artId,
        byte[] pixelData,
        int width,
        int height
    )
    {
        ArgumentNullException.ThrowIfNull(pixelData);

        if (!ShouldApplyCeHorizontalTileMirror(renderItemKind, artId))
            return;

        FlipPixelDataHorizontally(pixelData, width, height);
    }

    private static void FlipPixelDataHorizontally(byte[] pixelData, int width, int height)
    {
        const int bytesPerPixel = 4;
        if (width <= 1 || height <= 0)
            return;

        for (var row = 0; row < height; row++)
        {
            var rowStart = checked(row * width * bytesPerPixel);
            for (int left = 0, right = width - 1; left < right; left++, right--)
            {
                var leftIndex = rowStart + (left * bytesPerPixel);
                var rightIndex = rowStart + (right * bytesPerPixel);
                for (var channel = 0; channel < bytesPerPixel; channel++)
                    (pixelData[leftIndex + channel], pixelData[rightIndex + channel]) = (
                        pixelData[rightIndex + channel],
                        pixelData[leftIndex + channel]
                    );
            }
        }
    }

    private static (int CenterX, int CenterY) ApplyCeMirroredHotspotOffsets(int width, int centerX, int centerY) =>
        (width - centerX - 2, centerY);

    private static (int CenterX, int CenterY) ApplyCeWallPortalHotspotOffsets(
        ArtId artId,
        int width,
        int centerX,
        int centerY
    )
    {
        var rotationIndex = NormalizeWallPortalRotationIndex((int)((artId.Value >> ArtIdRotationShift) & 0x7u));
        var adjustedCenterX = centerX;
        var adjustedCenterY = centerY;

        // CE tig_art_frame_data applies an extra hotspot shift for the north/south-facing wall and portal
        // families before any mirror flip is evaluated.
        if (rotationIndex is < 2 or > 5)
        {
            adjustedCenterX -= 40;
            adjustedCenterY += 20;
        }

        if ((artId.Value & 0x1u) != 0)
            adjustedCenterX = width - adjustedCenterX - 2;

        return (adjustedCenterX, adjustedCenterY);
    }

    private ArtFile? TryGetResolvedArt(string assetPath)
    {
        if (
            _workspace.GameData.ArtsBySource.TryGetValue(assetPath, out var arts)
            && arts.FirstOrDefault() is { } storedArt
        )
            return storedArt;

        return _workspace.FindArt(assetPath);
    }

    private void Evict(IReadOnlyList<EditorMapRenderSpriteCacheKey> keys)
    {
        for (var index = 0; index < keys.Count; index++)
            _cache.Remove(keys[index]);
    }

    private static long EstimateRetainedBytes(EditorMapRenderSprite? sprite) =>
        sprite is null ? 1L : Math.Max(sprite.PixelData.Length, 1);

    private static int NormalizeFrameIndex(int requestedIndex, int itemCount)
    {
        if (itemCount <= 0)
            return 0;

        var normalizedIndex = requestedIndex % itemCount;
        return normalizedIndex < 0 ? normalizedIndex + itemCount : normalizedIndex;
    }

    private static bool IsSectorArtFamilyAssetPath(string assetPath) =>
        assetPath.StartsWith("art/tile/", StringComparison.OrdinalIgnoreCase)
        || assetPath.StartsWith("art/facade/", StringComparison.OrdinalIgnoreCase)
        || assetPath.StartsWith("art/roof/", StringComparison.OrdinalIgnoreCase)
        || assetPath.StartsWith("art/wall/", StringComparison.OrdinalIgnoreCase)
        || assetPath.StartsWith("art/light/", StringComparison.OrdinalIgnoreCase)
        || assetPath.StartsWith("art/portal/", StringComparison.OrdinalIgnoreCase);

    private static bool IsCompatibleFamily(EditorMapRenderQueueItemKind? kind, string assetPath)
    {
        if (kind is EditorMapRenderQueueItemKind.FloorTile)
        {
            return assetPath.StartsWith("art/tile/", StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith("art/facade/", StringComparison.OrdinalIgnoreCase);
        }
        if (kind is EditorMapRenderQueueItemKind.Roof)
            return assetPath.StartsWith("art/roof/", StringComparison.OrdinalIgnoreCase);

        return true;
    }
}
