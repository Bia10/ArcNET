namespace ArcNET.Diagnostics;

public sealed record class InventoryCreateExecutionResult(
    string DispatcherMode,
    string DispatcherSite,
    string ExecutionDetailText,
    string ResultText,
    ulong ItemHandle
);
