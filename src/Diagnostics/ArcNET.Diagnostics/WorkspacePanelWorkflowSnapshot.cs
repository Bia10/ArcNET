namespace ArcNET.Diagnostics;

public sealed record class WorkspacePanelWorkflowSnapshot(
    string PanelKey,
    string PanelDisplayName,
    string PanelDescription,
    string ShellSurfaceText,
    string WorkflowTitle,
    string WorkflowSummary
);
