namespace ArcNET.Diagnostics;

public sealed record SaveTypedReputationDeltaSnapshot(
    SaveTypedReputationDeltaKind Kind,
    int Count,
    IReadOnlyList<int> ChangedSlots
);
