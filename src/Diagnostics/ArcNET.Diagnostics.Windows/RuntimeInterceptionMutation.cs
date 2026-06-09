namespace ArcNET.Diagnostics.Windows;

public readonly record struct RuntimeInterceptionMutation(
    RuntimeInterceptionExecutionMode ExecutionMode,
    int CleanupBytes,
    uint ReturnEax,
    uint ReturnEdx,
    RuntimeInterceptionRegisters RegisterOverrides,
    uint RegisterOverrideMask,
    uint[] ArgumentOverrides,
    uint ArgumentOverrideMask
);
