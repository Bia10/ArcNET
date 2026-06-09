namespace ArcNET.Diagnostics;

public sealed record class FunctionAuditResultSnapshot(
    string Key,
    bool Success,
    string? Site,
    string? Resolution,
    string? Summary,
    string? Error
);
