namespace ArcNET.Diagnostics;

public sealed record class ModuleSymbolEntrySnapshot(
    string Name,
    string Site,
    uint Rva,
    string RvaText,
    string? Address,
    uint Size,
    string SizeText,
    int DuplicateNameCount
);
