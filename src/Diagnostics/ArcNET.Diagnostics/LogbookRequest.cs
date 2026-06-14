namespace ArcNET.Diagnostics;

public sealed record class LogbookRequest(
    AttachedSessionSnapshot Session,
    string HandleToken,
    string PageToken = "all",
    string? WorkspacePathOverride = null
)
{
    public string WorkspacePath =>
        string.IsNullOrWhiteSpace(WorkspacePathOverride) ? Session.LocalWorkspacePath : WorkspacePathOverride.Trim();
}
