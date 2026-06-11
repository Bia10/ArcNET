namespace ArcNET.Editor;

/// <summary>
/// Optional inputs for <see cref="EditorWorkspaceLoader"/>.
/// </summary>
public sealed class EditorWorkspaceLoadOptions
{
    /// <summary>
    /// Optional Arcanum installation root used for installation-type detection.
    /// Install-backed workspace loading supplies this automatically.
    /// </summary>
    public string? GameDirectory { get; init; }

    /// <summary>
    /// Optional module name used when one install-backed workspace should load only one specific module context.
    /// When omitted, install-backed loading keeps its existing whole-install behavior.
    /// </summary>
    public string? ModuleName { get; init; }

    /// <summary>
    /// Optional save directory. Must be supplied together with <see cref="SaveSlotName"/>.
    /// </summary>
    public string? SaveFolder { get; init; }

    /// <summary>
    /// Optional save slot name without extension. Must be supplied together with
    /// <see cref="SaveFolder"/>.
    /// </summary>
    public string? SaveSlotName { get; init; }

    /// <summary>
    /// When true, workspace loading eagerly parses ART metadata into the game-data store.
    /// Editors can set this to false and rely on the asset catalog plus lazy art loading for faster cold start.
    /// </summary>
    public bool LoadArtMetadata { get; init; } = true;
}
