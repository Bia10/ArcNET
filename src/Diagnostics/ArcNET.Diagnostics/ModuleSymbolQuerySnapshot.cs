using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class ModuleSymbolQuerySnapshot(
    DateTimeOffset GeneratedAtUtc,
    string ModulePath,
    string ModuleFileName,
    string? ModuleBase,
    RuntimeFingerprint? Fingerprint,
    string? Filter,
    int Limit,
    bool DuplicatesOnly,
    int FunctionCount,
    int UniqueNameCount,
    int DuplicateNameCount,
    IReadOnlyList<ModuleSymbolEntrySnapshot> Symbols
);
