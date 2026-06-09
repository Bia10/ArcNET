using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public static class WorkspaceService
{
    public static WorkspaceSnapshot Create(WorkspaceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dashboard = DashboardService.Create(
            new DashboardRequest(request.RuntimeProfile, request.HasModuleSymbols, request.RequestedProcessNames)
        );
        var timeline = TimelineService.Create(new TimelineRequest(request.RuntimeProfile, request.HasModuleSymbols));
        var functionBrowser = FunctionBrowserService.Create(
            new FunctionBrowserRequest(request.RuntimeProfile, request.HasModuleSymbols)
        );
        var objectExplorer = ObjectExplorerService.Create(
            new ObjectExplorerRequest(request.RuntimeProfile, request.HasModuleSymbols)
        );

        return new WorkspaceSnapshot(
            DateTimeOffset.UtcNow,
            request.RuntimeProfile,
            request.HasModuleSymbols,
            dashboard,
            CreatePanelWorkflows(dashboard.RecommendedPanels),
            timeline,
            functionBrowser,
            objectExplorer
        );
    }

    public static WorkspaceSnapshot CreateForRuntime(LiveRuntimeSnapshot runtime, bool hasModuleSymbols = false)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        return Create(CreateRequest(runtime.RuntimeProfile, hasModuleSymbols, [runtime.ProcessName]));
    }

    public static WorkspaceSnapshot CreateForSession(AttachedSessionSnapshot session, bool hasModuleSymbols = false)
    {
        ArgumentNullException.ThrowIfNull(session);
        return Create(CreateRequest(session.RuntimeProfile, hasModuleSymbols, [session.ProcessName]));
    }

    private static WorkspaceRequest CreateRequest(
        RuntimeProfileSnapshot runtimeProfile,
        bool hasModuleSymbols,
        IReadOnlyList<string> requestedProcessNames
    ) => new(runtimeProfile, hasModuleSymbols, requestedProcessNames);

    private static IReadOnlyList<WorkspacePanelWorkflowSnapshot> CreatePanelWorkflows(
        IReadOnlyList<PanelDescriptor> recommendedPanels
    ) => [.. recommendedPanels.Select(CreatePanelWorkflow)];

    private static WorkspacePanelWorkflowSnapshot CreatePanelWorkflow(PanelDescriptor panel) =>
        panel.Key switch
        {
            "home" => new WorkspacePanelWorkflowSnapshot(
                panel.Key,
                panel.DisplayName,
                panel.Description,
                "Workspace",
                "Runtime Radar, Launch Preview, and Active Session",
                "Discover live runtimes, validate one install path, attach or disconnect a session, and inspect support posture from the main workspace tab."
            ),
            "diagnostics" => new WorkspacePanelWorkflowSnapshot(
                panel.Key,
                panel.DisplayName,
                panel.Description,
                "Diagnostics",
                "Runtime Audit, Status, Interception, and Dumps",
                "Run runtime audits, inspect status, capture dumps, and drive advanced interception or read-side diagnostics from the diagnostics tab."
            ),
            "timeline" => new WorkspacePanelWorkflowSnapshot(
                panel.Key,
                panel.DisplayName,
                panel.Description,
                "Timeline",
                "Live Timeline Stream",
                "Start a watch preset, poll live events, and reuse detected handle candidates from the dedicated timeline tab."
            ),
            "objects" => new WorkspacePanelWorkflowSnapshot(
                panel.Key,
                panel.DisplayName,
                panel.Description,
                "Objects",
                "Live Object Explorer",
                "Inspect runtime handles, object groups, and structured detail cards through the object explorer tab."
            ),
            "sheets" => new WorkspacePanelWorkflowSnapshot(
                panel.Key,
                panel.DisplayName,
                panel.Description,
                "Diagnostics",
                "Prototype, Read, and Sheet",
                "Read one sheet value, capture a full live sheet snapshot, or diff two snapshots inside the diagnostics tab."
            ),
            "inventory" => new WorkspacePanelWorkflowSnapshot(
                panel.Key,
                panel.DisplayName,
                panel.Description,
                "Objects + Timeline",
                "Inventory Detail via Object Probe and Inventory Presets",
                "Inspect critter or container inventory fields through the object explorer and pair that with inventory-focused timeline presets for transfer and equip events."
            ),
            "scripts" => new WorkspacePanelWorkflowSnapshot(
                panel.Key,
                panel.DisplayName,
                panel.Description,
                "Diagnostics",
                "Script Attachments and Structured Reads",
                "Read live script attachment points and related getter-backed state from the diagnostics tab."
            ),
            "logbook" => new WorkspacePanelWorkflowSnapshot(
                panel.Key,
                panel.DisplayName,
                panel.Description,
                "Diagnostics",
                "Logbook Page Diagnostics",
                "Read live quests, rumors, reputations, background, injuries, and key-ring logbook pages from the diagnostics tab."
            ),
            "functions" => new WorkspacePanelWorkflowSnapshot(
                panel.Key,
                panel.DisplayName,
                panel.Description,
                "Functions",
                "Guided Action and Raw Native Call",
                "Browse known function targets, invoke guided actions, and run raw dispatcher-backed calls from the functions tab."
            ),
            "dumps" => new WorkspacePanelWorkflowSnapshot(
                panel.Key,
                panel.DisplayName,
                panel.Description,
                "Diagnostics",
                "Crash Dump Workflows",
                "Capture one manual runtime dump or manage automatic WER LocalDumps configuration from the diagnostics tab."
            ),
            _ => new WorkspacePanelWorkflowSnapshot(
                panel.Key,
                panel.DisplayName,
                panel.Description,
                "Coverage",
                "Catalog-only panel mapping",
                "This panel recommendation is visible in the coverage catalog but does not yet have a richer shell workflow mapping."
            ),
        };
}
