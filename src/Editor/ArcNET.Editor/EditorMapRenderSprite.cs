using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// Request used by host-facing map sprite sources when resolving one paintable ART frame.
/// </summary>
public sealed class EditorMapRenderSpriteRequest
{
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
/// Host-provided or workspace-backed ART frame resolver used when turning one normalized scene queue into paintable items.
/// </summary>
public interface IEditorMapRenderSpriteSource
{
    /// <summary>
    /// Resolves one paintable ART frame for <paramref name="artId"/>, or returns <see langword="null"/> when no binding exists.
    /// </summary>
    EditorMapRenderSprite? Resolve(ArtId artId, EditorMapRenderSpriteRequest? request = null);
}

/// <summary>
/// Workspace-backed sprite source that resolves bound <see cref="ArtId"/> values through one <see cref="EditorArtResolver"/>
/// and caches preview-ready frames for host rendering.
/// </summary>
public sealed class EditorWorkspaceMapRenderSpriteSource : IEditorMapRenderSpriteSource
{
    private readonly EditorWorkspace _workspace;
    private readonly EditorArtResolver _artResolver;
    private readonly EditorArtPreviewOptions _previewOptions;
    private readonly Dictionary<(ArtId ArtId, int RotationIndex, int FrameIndex), EditorMapRenderSprite?> _cache =
        new();

    public EditorWorkspaceMapRenderSpriteSource(
        EditorWorkspace workspace,
        EditorArtResolver artResolver,
        EditorArtPreviewOptions? previewOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(artResolver);

        _workspace = workspace;
        _artResolver = artResolver;
        _previewOptions = previewOptions ?? new EditorArtPreviewOptions();
    }

    /// <summary>
    /// Total number of cached ART frame lookups stored by this source.
    /// </summary>
    public int CachedFrameCount => _cache.Count;

    /// <inheritdoc />
    public EditorMapRenderSprite? Resolve(ArtId artId, EditorMapRenderSpriteRequest? request = null)
    {
        if (artId.Value == 0)
            return null;

        var effectiveRequest = request ?? new EditorMapRenderSpriteRequest();
        var cacheKey = (artId, effectiveRequest.RotationIndex, effectiveRequest.FrameIndex);
        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        var resolved = ResolveCore(artId, effectiveRequest);
        _cache[cacheKey] = resolved;
        return resolved;
    }

    private EditorMapRenderSprite? ResolveCore(ArtId artId, EditorMapRenderSpriteRequest request)
    {
        var assetPath = _artResolver.FindAssetPath(artId);
        if (string.IsNullOrWhiteSpace(assetPath))
            return null;

        var preview = _workspace.CreateArtPreview(assetPath, _previewOptions);
        var frame = preview.Frames.FirstOrDefault(candidate =>
            candidate.RotationIndex == request.RotationIndex && candidate.FrameIndex == request.FrameIndex
        );
        if (frame is null)
            return null;

        return new EditorMapRenderSprite
        {
            ArtId = artId,
            AssetPath = assetPath,
            RotationIndex = frame.RotationIndex,
            FrameIndex = frame.FrameIndex,
            Width = frame.Width,
            Height = frame.Height,
            Stride = frame.Stride,
            CenterX = frame.Header.CenterX,
            CenterY = frame.Header.CenterY,
            FrameRate = preview.FrameRate,
            PixelFormat = preview.PixelFormat,
            PixelData = frame.PixelData,
        };
    }
}
