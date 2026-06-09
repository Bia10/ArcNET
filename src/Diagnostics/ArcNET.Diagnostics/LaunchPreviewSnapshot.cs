using ArcNET.Patch;

namespace ArcNET.Diagnostics;

public sealed record class LaunchPreviewSnapshot(
    bool CanLaunch,
    string Summary,
    string? Error,
    ArcanumExecutableKind? ExecutableKind,
    string? ExecutablePath,
    string? WorkingDirectory,
    IReadOnlyList<string> Arguments,
    IReadOnlyList<string> EnvironmentVariables
);
