namespace ArcNET.Diagnostics;

public sealed record class HookAuditResultSnapshot(
    string Key,
    string Area,
    HookBindAuditSnapshot Bind,
    HookPassAuditSnapshot? Watch,
    HookPassAuditSnapshot? Intercept
);
