namespace ArcNET.Diagnostics;

public sealed record class CrashDumpWriteSnapshot(
    DateTimeOffset GeneratedAtUtc,
    int ProcessId,
    string ProcessName,
    string ModulePath,
    string ModuleBase,
    string OutputPath,
    CrashDumpKind DumpKind
);
