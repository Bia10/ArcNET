namespace ArcNET.Diagnostics;

public sealed record class CrashDumpAutoConfigurationSnapshot(
    bool IsEnabled,
    string ProcessExecutableName,
    string Scope,
    string RegistryPath,
    string? DumpFolder,
    CrashDumpKind? DumpKind,
    int? DumpCount
);
