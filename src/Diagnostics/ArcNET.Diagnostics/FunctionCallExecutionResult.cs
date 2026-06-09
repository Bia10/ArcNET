namespace ArcNET.Diagnostics;

public sealed record class FunctionCallExecutionResult(
    string DispatcherMode,
    string DispatcherSite,
    string TargetAddressText,
    uint ResultEax,
    uint ResultEdx,
    string CompletionState
);
