namespace ArcNET.Diagnostics;

public sealed record class InterceptEventSnapshot(
    DateTimeOffset TimestampUtc,
    uint Sequence,
    string CallerSite,
    string ReturnAddressText,
    string CallerRvaText,
    string EflagsText,
    InterceptRegistersSnapshot Registers,
    IReadOnlyList<string> StackDwords,
    IReadOnlyList<InterceptPotentialHandleSnapshot> PotentialHandles,
    IReadOnlyList<InterceptDereferenceSnapshot> Dereferences
);
