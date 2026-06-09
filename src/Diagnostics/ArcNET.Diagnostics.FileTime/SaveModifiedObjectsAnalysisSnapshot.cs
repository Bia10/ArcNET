namespace ArcNET.Diagnostics;

public sealed record SaveModifiedObjectsAnalysisSnapshot(
    IReadOnlyList<SaveModifiedObjectEntrySnapshot> Entries,
    string? TerminalWarning
);
