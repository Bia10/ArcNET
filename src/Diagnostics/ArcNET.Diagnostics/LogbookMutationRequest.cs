namespace ArcNET.Diagnostics;

public sealed record class LogbookMutationRequest(
    AttachedSessionSnapshot Session,
    string TargetHandleToken,
    LogbookMutationKind Kind,
    string EntryTokenText,
    string ValueTokenText,
    string AuxiliaryTokenText,
    string TimeoutMillisecondsText,
    string? WorkspacePathOverride = null
)
{
    public string WorkspacePath =>
        string.IsNullOrWhiteSpace(WorkspacePathOverride) ? Session.LocalWorkspacePath : WorkspacePathOverride.Trim();
}
