using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// Sprite-resolution coverage for one paintable map scene.
/// </summary>
public sealed class EditorMapRenderSpriteCoverage
{
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
    public bool IsComplete => UnresolvedArtIds.Count == 0;
}
