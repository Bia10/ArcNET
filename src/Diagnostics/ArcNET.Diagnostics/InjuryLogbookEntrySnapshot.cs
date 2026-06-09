namespace ArcNET.Diagnostics;

public readonly record struct InjuryLogbookEntrySnapshot(
    int SlotIndex,
    int DescriptionId,
    string SourceName,
    int InjuryType,
    string InjuryTypeName,
    bool Active,
    string StateText,
    string SummaryText
);
