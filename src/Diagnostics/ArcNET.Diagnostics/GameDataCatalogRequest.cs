namespace ArcNET.Diagnostics;

public sealed record class GameDataCatalogRequest(AttachedSessionSnapshot Session, string? WorkspacePathOverride = null)
{
    public string WorkspacePath =>
        string.IsNullOrWhiteSpace(WorkspacePathOverride) ? Session.LocalWorkspacePath : WorkspacePathOverride.Trim();
}
