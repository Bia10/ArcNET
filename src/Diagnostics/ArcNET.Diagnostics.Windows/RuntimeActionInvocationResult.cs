namespace ArcNET.Diagnostics.Windows;

public sealed record class RuntimeActionInvocationResult(
    string FunctionKey,
    string DispatcherMode,
    string DispatcherSite,
    string TargetAddressText,
    uint ResultEax,
    uint ResultEdx,
    string State
);
