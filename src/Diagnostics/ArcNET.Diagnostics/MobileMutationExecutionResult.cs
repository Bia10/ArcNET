namespace ArcNET.Diagnostics;

public sealed record class MobileMutationExecutionResult(
    string DispatcherMode,
    string DispatcherSite,
    string ExecutionDetailText,
    string ResultText,
    ulong RelatedHandle
);
