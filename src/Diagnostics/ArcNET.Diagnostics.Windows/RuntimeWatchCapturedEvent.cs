namespace ArcNET.Diagnostics.Windows;

public readonly record struct RuntimeWatchCapturedEvent(
    RuntimeWatchHookDefinition Definition,
    uint Sequence,
    uint ReturnAddress,
    uint CallerRva,
    RuntimeWatchStackCapture StackDwords
);
