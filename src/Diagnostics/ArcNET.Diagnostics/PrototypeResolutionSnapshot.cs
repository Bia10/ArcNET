using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class PrototypeResolutionSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    string Token,
    int? ProtoNumber,
    string? DisplayName,
    string? AssetPath,
    ulong? Handle,
    string HandleText,
    string ResolutionSource,
    ResolvedObjectSnapshot? ResolvedObject,
    IReadOnlyList<string> Notes
);
