namespace ArcNET.Editor;

/// <summary>
/// Asset-centric dependency summary for one indexed workspace asset.
/// </summary>
public sealed class EditorAssetDependencySummary
{
    /// <summary>
    /// Asset that owns this summary.
    /// </summary>
    public required EditorAssetEntry Asset { get; init; }

    /// <summary>
    /// Owning map directory when the asset lives under <c>maps/&lt;name&gt;/</c>; otherwise <see langword="null"/>.
    /// </summary>
    public string? MapName { get; init; }

    /// <summary>
    /// Defined proto number when the asset path identifies one proto asset; otherwise <see langword="null"/>.
    /// </summary>
    public int? DefinedProtoNumber { get; init; }

    /// <summary>
    /// Defined script identifier when the asset path identifies one compiled script asset; otherwise <see langword="null"/>.
    /// </summary>
    public int? DefinedScriptId { get; init; }

    /// <summary>
    /// Defined dialog identifier when the asset path identifies one dialog asset; otherwise <see langword="null"/>.
    /// </summary>
    public int? DefinedDialogId { get; init; }

    /// <summary>
    /// Proto-number references discovered inside the asset.
    /// </summary>
    public required IReadOnlyList<EditorProtoReference> ProtoReferences { get; init; }

    /// <summary>
    /// Script-identifier references discovered inside the asset.
    /// </summary>
    public required IReadOnlyList<EditorScriptReference> ScriptReferences { get; init; }

    /// <summary>
    /// Art-resource references discovered inside the asset.
    /// </summary>
    public required IReadOnlyList<EditorArtReference> ArtReferences { get; init; }

    /// <summary>
    /// Assets that currently reference <see cref="DefinedProtoNumber"/> when this asset defines a proto; otherwise empty.
    /// </summary>
    public required IReadOnlyList<EditorProtoReference> IncomingProtoReferences { get; init; }

    /// <summary>
    /// Assets that currently reference <see cref="DefinedScriptId"/> when this asset defines a script; otherwise empty.
    /// </summary>
    public required IReadOnlyList<EditorScriptReference> IncomingScriptReferences { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the asset references at least one proto, script, or art identifier.
    /// </summary>
    public bool HasDependencies => ProtoReferences.Count > 0 || ScriptReferences.Count > 0 || ArtReferences.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when one or more indexed assets currently reference this asset's defined proto or script identifier.
    /// </summary>
    public bool HasIncomingReferences => IncomingProtoReferences.Count > 0 || IncomingScriptReferences.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when the asset participates in the current dependency graph either as a source or a target.
    /// </summary>
    public bool HasRelationships => HasDependencies || HasIncomingReferences;
}
