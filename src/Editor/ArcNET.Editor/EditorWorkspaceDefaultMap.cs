namespace ArcNET.Editor;

/// <summary>
/// How one workspace default-map resolution was chosen.
/// </summary>
public enum EditorWorkspaceDefaultMapSource
{
    /// <summary>
    /// The loaded save-slot map identifier matched one indexed map name.
    /// </summary>
    SaveInfoMapId = 0,

    /// <summary>
    /// One conventional <c>map01</c>-style module entry map was present.
    /// </summary>
    ConventionalMap01 = 1,

    /// <summary>
    /// Only one indexed map was available in the workspace.
    /// </summary>
    SingleIndexedMap = 2,

    /// <summary>
    /// The first indexed map name was used as a conservative fallback.
    /// </summary>
    FirstIndexedMap = 3,
}

/// <summary>
/// Host-facing default-map resolution for one loaded workspace or module.
/// </summary>
public sealed class EditorWorkspaceDefaultMap
{
    /// <summary>
    /// Resolved indexed map directory name.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// Strategy that selected <see cref="MapName"/>.
    /// </summary>
    public required EditorWorkspaceDefaultMapSource Source { get; init; }

    /// <summary>
    /// Save-slot map identifier consulted during resolution, when one loaded save was present.
    /// </summary>
    public int? SaveMapId { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the resolved map came from one conservative fallback instead of
    /// one save-linked or conventional module entry-map match.
    /// </summary>
    public bool IsFallback =>
        Source is EditorWorkspaceDefaultMapSource.SingleIndexedMap or EditorWorkspaceDefaultMapSource.FirstIndexedMap;
}
