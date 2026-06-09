namespace ArcNET.Diagnostics;

public sealed record class ProcessCandidateSnapshot(
    string ProcessName,
    string DisplayName,
    bool IsRunning,
    int RunningInstanceCount,
    string StatusText
);
