namespace ArcNET.Diagnostics;

public sealed record class LogbookMutationExecutionResult(
    string DispatcherMode,
    string DispatcherSite,
    string ExecutionDetailText,
    string ResultText,
    bool NoMutation = false
);
