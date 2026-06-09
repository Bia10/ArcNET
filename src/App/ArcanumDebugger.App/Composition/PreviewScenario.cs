using ArcNET.Diagnostics;

namespace ArcanumDebugger.App.Composition;

public sealed record class ArcanumDebuggerPreviewScenario(
    string Key,
    string DisplayName,
    string Summary,
    WorkspaceRequest WorkspaceRequest
);
