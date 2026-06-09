using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class WorkspaceRequest(
    RuntimeProfileSnapshot RuntimeProfile,
    bool HasModuleSymbols,
    IReadOnlyList<string> RequestedProcessNames
);
