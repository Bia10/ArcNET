namespace ArcNET.Diagnostics;

public sealed record PlayerSarTransitionReportSnapshot(
    IReadOnlyList<PlayerSarTransitionReportEntrySnapshot> Transitions
);
