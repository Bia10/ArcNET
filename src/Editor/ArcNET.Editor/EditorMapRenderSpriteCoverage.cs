using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// Sprite-resolution coverage for one paintable map scene.
/// </summary>
public sealed class EditorMapRenderSpriteCoverage
{
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
