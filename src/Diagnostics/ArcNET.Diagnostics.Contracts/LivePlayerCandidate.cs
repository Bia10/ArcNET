namespace ArcNET.Diagnostics.Contracts;

public readonly record struct LivePlayerCandidate(
    ulong Handle,
    string HandleHex,
    string CandidateKind,
    string DisplayValue,
    string ResolutionSource,
    string? ObjectTypeName,
    string? ObjectIdLabel,
    string? PrototypeIdLabel,
    int? ProtoNumber,
    string? PrototypeHandle
);
