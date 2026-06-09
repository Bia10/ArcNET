namespace ArcNET.Diagnostics;

public sealed record class NativeReadSnapshot(
    string FunctionKey,
    string FunctionSite,
    string FunctionSummary,
    string DispatcherMode,
    string DispatcherSite,
    string CompletionState,
    int Int32Value,
    string ResultEaxText,
    string ResultEdxText
);
