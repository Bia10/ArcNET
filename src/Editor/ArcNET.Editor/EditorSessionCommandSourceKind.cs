namespace ArcNET.Editor;

/// <summary>
/// Underlying routing source chosen for one default session command.
/// </summary>
public enum EditorSessionCommandSourceKind
{
    /// <summary>
    /// The command routes through staged local session history.
    /// </summary>
    Staged = 0,

    /// <summary>
    /// The command routes through applied session history.
    /// </summary>
    History = 1,
}
