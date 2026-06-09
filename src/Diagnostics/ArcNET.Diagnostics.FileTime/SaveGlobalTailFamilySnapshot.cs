namespace ArcNET.Diagnostics;

public readonly record struct SaveGlobalTailFamilySnapshot(
    IReadOnlyList<int> Slots,
    int RowCount,
    int SectionCount,
    string Sequence
);
