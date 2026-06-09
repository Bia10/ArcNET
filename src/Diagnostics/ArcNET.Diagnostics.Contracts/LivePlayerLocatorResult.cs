namespace ArcNET.Diagnostics.Contracts;

public readonly record struct LivePlayerLocatorResult(
    ulong? AutoResolvedHandle,
    string ResolutionSource,
    string Summary,
    IReadOnlyList<LivePlayerCandidate> LivePlayerCandidates,
    IReadOnlyList<LivePlayerCandidate> PrototypeTemplates,
    IReadOnlyList<string> Notes
);
