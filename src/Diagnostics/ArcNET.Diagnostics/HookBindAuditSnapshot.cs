namespace ArcNET.Diagnostics;

public sealed record class HookBindAuditSnapshot(bool Success, string Site, string? Error);
