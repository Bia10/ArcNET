namespace ArcNET.Diagnostics;

public readonly record struct RuntimeWatchCapturedEvent(
    RuntimeWatchHookDefinition Definition,
    uint Sequence,
    uint ReturnAddress,
    uint CallerRva,
    RuntimeWatchStackCapture StackDwords
);
