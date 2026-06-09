using ArcNET.Patch;

namespace ArcNET.Diagnostics;

public sealed record class LaunchSessionRequest(
    string InstallPath,
    ArcanumExecutableKind ExecutableKind,
    bool LaunchWindowed,
    TimeSpan? AttachTimeout = null
);
