using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// Sprite-resolution coverage for one paintable map scene.
/// </summary>
public sealed class EditorMapRenderSpriteCoverage
{
    /// <summary>
    /// A shared empty coverage instance for scenes that need no sprite resolution tracking.
    /// </summary>
    public static EditorMapRenderSpriteCoverage Empty { get; } =
        new()
        {
            ReferencedSpriteReferenceCount = 0,
            ResolvedSpriteReferenceCount = 0,
            UnresolvedSpriteReferenceCount = 0,
            ReferencedArtIds = [],
            ResolvedArtIds = [],
            UnresolvedArtIds = [],
        };

    /// <summary>
    /// Distinct sprite-bearing paintable references, counting render-kind-specific uses of the same ART identifier separately.
    /// </summary>
    public required int ReferencedSpriteReferenceCount { get; init; }

    /// <summary>
    /// Distinct sprite-bearing paintable references that the configured sprite source successfully resolved.
    /// </summary>
    public required int ResolvedSpriteReferenceCount { get; init; }

    /// <summary>
    /// Distinct sprite-bearing paintable references that remained unresolved by the configured sprite source.
    /// </summary>
    public required int UnresolvedSpriteReferenceCount { get; init; }

    /// <summary>
    /// Distinct sprite-frame requests referenced by paintable scene items.
    /// </summary>
    public IReadOnlyList<EditorMapRenderSpriteCoverageReference> ReferencedSpriteReferences { get; init; } = [];

    /// <summary>
    /// Distinct sprite-frame requests that the configured sprite source successfully resolved.
    /// </summary>
    public IReadOnlyList<EditorMapRenderSpriteCoverageReference> ResolvedSpriteReferences { get; init; } = [];

    /// <summary>
    /// Distinct sprite-frame requests that remained unresolved by the configured sprite source.
    /// </summary>
    public IReadOnlyList<EditorMapRenderSpriteCoverageReference> UnresolvedSpriteReferences { get; init; } = [];

    /// <summary>
    /// Distinct ART identifiers referenced by paintable scene items that can carry sprites.
    /// </summary>
    public required IReadOnlyList<ArtId> ReferencedArtIds { get; init; }

    /// <summary>
    /// Distinct referenced ART identifiers that the configured sprite source successfully resolved.
    /// </summary>
    public required IReadOnlyList<ArtId> ResolvedArtIds { get; init; }

    /// <summary>
    /// Distinct referenced ART identifiers that remained unresolved by the configured sprite source.
    /// </summary>
    public required IReadOnlyList<ArtId> UnresolvedArtIds { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when every referenced ART identifier resolved to one paintable sprite.
    /// </summary>
    public bool IsComplete => UnresolvedSpriteReferenceCount == 0;
}
