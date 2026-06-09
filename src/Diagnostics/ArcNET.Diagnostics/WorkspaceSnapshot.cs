using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class WorkspaceSnapshot(
    DateTimeOffset GeneratedAtUtc,
    RuntimeProfileSnapshot RuntimeProfile,
    bool HasModuleSymbols,
    DashboardSnapshot Dashboard,
    IReadOnlyList<WorkspacePanelWorkflowSnapshot> PanelWorkflows,
    TimelineSnapshot Timeline,
    FunctionBrowserSnapshot FunctionBrowser,
    ObjectExplorerSnapshot ObjectExplorer
);
