namespace ArcNET.Editor;

/// <summary>
/// Host-facing dependency impact summary for one staged session scope or pending session head.
/// </summary>
public sealed class EditorSessionImpactSummary
{
    /// <summary>
    /// High-level staged change categories represented by <see cref="DirectTargets"/>.
    /// </summary>
    public required IReadOnlyList<EditorSessionChangeKind> DirectKinds { get; init; }

    /// <summary>
    /// Direct targets currently staged for apply/save in stable order.
    /// </summary>
    public required IReadOnlyList<string> DirectTargets { get; init; }

    /// <summary>
    /// High-level asset categories represented by <see cref="RelatedAssetPaths"/>.
    /// </summary>
    public required IReadOnlyList<EditorSessionChangeKind> RelatedKinds { get; init; }

    /// <summary>
    /// Related asset paths discovered through incoming proto or script references to the direct targets.
    /// Direct targets are excluded from this list.
    /// </summary>
    public required IReadOnlyList<string> RelatedAssetPaths { get; init; }

    /// <summary>
    /// Map names touched either directly or through related referencing assets.
    /// </summary>
    public required IReadOnlyList<string> MapNames { get; init; }

    /// <summary>
    /// Proto numbers defined by one or more direct targets.
    /// </summary>
    public required IReadOnlyList<int> DefinedProtoNumbers { get; init; }

    /// <summary>
    /// Script identifiers defined by one or more direct targets.
    /// </summary>
    public required IReadOnlyList<int> DefinedScriptIds { get; init; }

    /// <summary>
    /// Dialog identifiers defined by one or more direct targets.
    /// </summary>
    public required IReadOnlyList<int> DefinedDialogIds { get; init; }

    /// <summary>
    /// Proto numbers referenced by the current staged direct targets.
    /// </summary>
    public required IReadOnlyList<int> ReferencedProtoNumbers { get; init; }

    /// <summary>
    /// Script identifiers referenced by the current staged direct targets.
    /// </summary>
    public required IReadOnlyList<int> ReferencedScriptIds { get; init; }

    /// <summary>
    /// Art identifiers referenced by the current staged direct targets.
    /// </summary>
    public required IReadOnlyList<uint> ReferencedArtIds { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this summary contains one or more direct targets.
    /// </summary>
    public bool HasDirectTargets => DirectTargets.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when this summary discovered one or more related assets beyond the direct targets.
    /// </summary>
    public bool HasRelatedAssets => RelatedAssetPaths.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when this summary resolved one or more touched maps.
    /// </summary>
    public bool HasMapCoverage => MapNames.Count > 0;
}
