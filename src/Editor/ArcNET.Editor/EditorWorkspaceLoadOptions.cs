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
    /// Optional save directory. Must be supplied together with <see cref="SaveSlotName"/>.
    /// </summary>
    public string? SaveFolder { get; init; }

    /// <summary>
    /// Optional save slot name without extension. Must be supplied together with
    /// <see cref="SaveFolder"/>.
    /// </summary>
    public string? SaveSlotName { get; init; }
}
