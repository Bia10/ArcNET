namespace ArcNET.Diagnostics.Contracts;

public sealed record class RuntimeFingerprint(
    string ProcessName,
    int ProcessId,
    RuntimeKind RuntimeKind,
    string ModuleFileName,
    string ModulePath,
    string ModuleBase,
    int ModuleSize,
    long ModuleFileSize,
    DateTime ModuleLastWriteTimeUtc
);
