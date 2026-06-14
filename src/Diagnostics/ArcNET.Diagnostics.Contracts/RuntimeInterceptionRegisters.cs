namespace ArcNET.Diagnostics;

public readonly record struct RuntimeInterceptionRegisters(
    uint Edi,
    uint Esi,
    uint Ebp,
    uint OriginalEsp,
    uint Ebx,
    uint Edx,
    uint Ecx,
    uint Eax
);
