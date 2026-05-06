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
    public required uint FrameRate { get; init; }
    public required EditorArtPreviewPixelFormat PixelFormat { get; init; }
    public required byte[] PixelData { get; init; }
}

/// <summary>
/// Lightweight sprite-frame metrics that can be used for layout and viewport indexing without decoding frame pixels.
/// </summary>
public sealed class EditorMapRenderSpriteMetrics
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int CenterX { get; init; }
    public required int CenterY { get; init; }

    public static EditorMapRenderSpriteMetrics FromSprite(EditorMapRenderSprite sprite)
    {
        ArgumentNullException.ThrowIfNull(sprite);

        return new EditorMapRenderSpriteMetrics
        {
            Width = sprite.Width,
            Height = sprite.Height,
            CenterX = sprite.CenterX,
            CenterY = sprite.CenterY,
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
    private readonly EditorWorkspace _workspace;
    private readonly EditorArtResolver _artResolver;
    private readonly EditorArtPreviewOptions _previewOptions;
    private readonly object _cacheGate = new();
    private readonly Dictionary<EditorMapRenderSpriteCacheKey, EditorMapRenderSprite?> _cache = [];
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

    /// <inheritdoc />
    public EditorMapRenderSpriteMetrics? GetSpriteMetrics(ArtId artId, EditorMapRenderSpriteRequest? request = null)
    {
        if (artId.Value == 0)
            return null;

        var effectiveRequest = request ?? new EditorMapRenderSpriteRequest();
        var assetPath = TryResolveAssetPath(artId, effectiveRequest);
        if (assetPath is null)
            return null;

        return CreateResolvedSpriteMetrics(artId, effectiveRequest, assetPath);
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

    private EditorMapRenderSprite? ResolveCore(ArtId artId, EditorMapRenderSpriteRequest request)
    {
        var assetPath = TryResolveAssetPath(artId, request);
        if (assetPath is null)
            return null;

        return CreateResolvedSprite(artId, request, assetPath);
    }

    private string? TryResolveAssetPath(ArtId artId, EditorMapRenderSpriteRequest request)
    {
        if (
            request.RenderItemKind is EditorMapRenderQueueItemKind.FloorTile or EditorMapRenderQueueItemKind.Roof
            && IsSectorArtId(artId.Value)
        )
        {
            if (_workspace.TryResolveMapRenderArtAssetPath(artId, request.RenderItemKind, out var renderItemAssetPath))
                return renderItemAssetPath;

            var lowSectorFallbackAssetPath = _artResolver.FindAssetPath(artId);
            if (
                string.IsNullOrWhiteSpace(lowSectorFallbackAssetPath)
                || IsSectorArtFamilyAssetPath(lowSectorFallbackAssetPath)
            )
                return null;

            return lowSectorFallbackAssetPath;
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

        var effectiveRotationIndex = ResolveEffectiveRotationIndex(artId, assetPath, request.RotationIndex);
        var rotationIndex = NormalizeFrameIndex(effectiveRotationIndex, art.EffectiveRotationCount);
        var frameIndex = NormalizeFrameIndex(request.FrameIndex, checked((int)art.FrameCount));
        var frame = EditorArtPreviewBuilder.BuildFrame(art, rotationIndex, frameIndex, _previewOptions);
        var (centerX, centerY) = AdjustSpriteCenter(
            assetPath,
            effectiveRotationIndex,
            frame.Header.CenterX,
            frame.Header.CenterY
        );

        return new EditorMapRenderSprite
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
            FrameRate = art.FrameRate,
            PixelFormat = _previewOptions.PixelFormat,
            PixelData = frame.PixelData,
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

        var effectiveRotationIndex = ResolveEffectiveRotationIndex(artId, assetPath, request.RotationIndex);
        var rotationIndex = NormalizeFrameIndex(effectiveRotationIndex, art.EffectiveRotationCount);
        var frameIndex = NormalizeFrameIndex(request.FrameIndex, checked((int)art.FrameCount));
        var header = art.Frames[rotationIndex][frameIndex].Header;
        var (centerX, centerY) = AdjustSpriteCenter(assetPath, effectiveRotationIndex, header.CenterX, header.CenterY);

        return new EditorMapRenderSpriteMetrics
        {
            Width = checked((int)header.Width),
            Height = checked((int)header.Height),
            CenterX = centerX,
            CenterY = centerY,
        };
    }

    private static int ResolveEffectiveRotationIndex(ArtId artId, string assetPath, int requestedRotationIndex) =>
        UsesArtIdRotationForWallPortal(assetPath, artId)
            ? NormalizeWallPortalRotationIndex((int)((artId.Value >> 11) & 0x7u))
            : requestedRotationIndex;

    private static (int CenterX, int CenterY) AdjustSpriteCenter(
        string assetPath,
        int requestedRotationIndex,
        int centerX,
        int centerY
    )
    {
        if (!RequiresCeWallPortalHotspotAdjustment(assetPath))
            return (centerX, centerY);

        var normalizedRotationIndex = NormalizeWallPortalRotationIndex(requestedRotationIndex);
        return normalizedRotationIndex is < 2 or > 5 ? (centerX - 40, centerY + 20) : (centerX, centerY);
    }

    private static bool RequiresCeWallPortalHotspotAdjustment(string assetPath) =>
        assetPath.StartsWith("art/wall/", StringComparison.OrdinalIgnoreCase)
        || assetPath.StartsWith("art/portal/", StringComparison.OrdinalIgnoreCase);

    private static bool UsesArtIdRotationForWallPortal(string assetPath, ArtId artId) =>
        RequiresCeWallPortalHotspotAdjustment(assetPath) && (artId.Value & ArtTypeMask) is WallArtType or PortalArtType;

    private static int NormalizeWallPortalRotationIndex(int rotationIndex)
    {
        var normalizedRotationIndex = rotationIndex % 8;
        return normalizedRotationIndex < 0 ? normalizedRotationIndex + 8 : normalizedRotationIndex;
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
}
