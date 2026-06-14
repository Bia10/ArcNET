namespace ArcNET.Diagnostics;

public sealed record class RunningProcessInfo(
    string ProcessName,
    int ProcessId,
    string ModuleFileName,
    string ModulePath,
    nint ModuleBase,
    int ModuleSize,
    string? RuntimeWorkspacePathHint = null
);
