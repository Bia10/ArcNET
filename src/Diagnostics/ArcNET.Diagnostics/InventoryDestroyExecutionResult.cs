namespace ArcNET.Diagnostics;

public sealed record class InventoryDestroyExecutionResult(
    string DispatcherMode,
    string DispatcherSite,
    string ExecutionDetailText,
    string ResultText,
    ulong ParentHandle
);
