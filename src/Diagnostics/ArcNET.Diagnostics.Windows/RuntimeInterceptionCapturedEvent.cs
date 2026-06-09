namespace ArcNET.Diagnostics.Windows;

public readonly record struct RuntimeInterceptionCapturedEvent(
    RuntimeInterceptionDefinition Definition,
    uint Sequence,
    uint ReturnAddress,
    uint CallerRva,
    uint Eflags,
    RuntimeInterceptionRegisters Registers,
    uint[] StackDwords
);
