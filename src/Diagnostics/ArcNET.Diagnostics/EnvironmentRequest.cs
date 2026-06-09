using ArcNET.Patch;

namespace ArcNET.Diagnostics;

public sealed record class EnvironmentRequest(
    IReadOnlyList<string> RequestedProcessNames,
    string? InstallPath,
    ArcanumExecutableKind LaunchExecutableKind,
    bool LaunchWindowed
);
