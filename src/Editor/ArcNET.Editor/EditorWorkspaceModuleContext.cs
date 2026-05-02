namespace ArcNET.Editor;

/// <summary>
/// Resolved module-backed workspace context.
/// </summary>
public sealed class EditorWorkspaceModuleContext
{
    /// <summary>
    /// Canonical module name used for loose-directory and archive resolution.
    /// </summary>
    public required string ModuleName { get; init; }

    /// <summary>
    /// Loose module content directory, typically <c>modules/&lt;name&gt;</c>.
    /// </summary>
    public required string ModuleDirectory { get; init; }

    /// <summary>
    /// Optional module save directory, typically <c>modules/&lt;name&gt;/Save</c>.
    /// </summary>
    public string? SaveDirectory { get; init; }

    /// <summary>
    /// Archive paths that participated in the winning module context, in overlay order.
    /// </summary>
    public required IReadOnlyList<string> ArchivePaths { get; init; }
}
