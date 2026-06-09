namespace ArcNET.Diagnostics;

public readonly record struct SaveGlobalFrontMatterFamilySnapshot(
    IReadOnlyList<int> Slots,
    int RowCount,
    int SectionCount,
    string Sequence
);
