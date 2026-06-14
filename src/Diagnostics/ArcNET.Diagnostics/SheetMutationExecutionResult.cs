namespace ArcNET.Diagnostics;

public sealed record class SheetMutationExecutionResult(
    string DispatcherMode,
    string DispatcherSite,
    string ExecutionDetailText,
    string ResultText,
    bool NoMutation = false
);
