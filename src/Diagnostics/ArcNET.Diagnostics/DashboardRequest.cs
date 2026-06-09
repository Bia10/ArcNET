using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class DashboardRequest(
    RuntimeProfileSnapshot RuntimeProfile,
    bool HasModuleSymbols,
    IReadOnlyList<string> RequestedProcessNames
);
