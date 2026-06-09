namespace ArcNET.Patch;

/// <summary>
/// Concrete executable, argument, and environment state that ArcNET will use to launch Arcanum.
/// </summary>
public sealed record class ArcanumLaunchPlan(
    ArcanumExecutableKind ExecutableKind,
    string ExecutablePath,
    string WorkingDirectory,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string> EnvironmentVariables
);
