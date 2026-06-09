using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class ResolvedObjectSnapshot(
    string HandleText,
    string DisplayValue,
    string? Name,
    string? ObjectType,
    int? ProtoNumber,
    string ResolutionSource,
    LiveObjectIdentity RuntimeIdentity
);
