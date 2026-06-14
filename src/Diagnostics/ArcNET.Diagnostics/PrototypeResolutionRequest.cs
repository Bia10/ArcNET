namespace ArcNET.Diagnostics;

public sealed record class PrototypeResolutionRequest(
    AttachedSessionSnapshot Session,
    string PrototypeText,
    string? WorkspacePathOverride = null
)
{
    public string WorkspacePath =>
        string.IsNullOrWhiteSpace(WorkspacePathOverride) ? Session.LocalWorkspacePath : WorkspacePathOverride.Trim();
}
