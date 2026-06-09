namespace ArcNET.Diagnostics;

public sealed record class DispatcherAuditSnapshot(bool Success, string? Mode, string? Site, string? Error);
