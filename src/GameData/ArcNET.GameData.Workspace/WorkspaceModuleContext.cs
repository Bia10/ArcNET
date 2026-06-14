namespace ArcNET.GameData.Workspace;

/// <summary>
/// Resolved module-backed workspace context.
/// </summary>
public sealed class WorkspaceModuleContext
{
    /// <summary>
    /// Creates one module-backed workspace context from a module directory plus the archive paths
    /// that participated in the winning load order.
    /// </summary>
    public static WorkspaceModuleContext Create(string moduleDirectory, IReadOnlyList<string>? archivePaths = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleDirectory);

        var resolvedModuleDirectory = Path.GetFullPath(moduleDirectory);
        var saveDirectory = Path.Combine(resolvedModuleDirectory, "Save");
        return new WorkspaceModuleContext
        {
            ModuleName = Path.GetFileName(
                resolvedModuleDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            ),
            ModuleDirectory = resolvedModuleDirectory,
            SaveDirectory = Directory.Exists(saveDirectory) ? saveDirectory : null,
            ArchivePaths = archivePaths is null ? [] : [.. archivePaths],
        };
    }

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
