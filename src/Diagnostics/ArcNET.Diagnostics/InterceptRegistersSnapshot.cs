namespace ArcNET.Diagnostics;

public sealed record class InterceptRegistersSnapshot(
    string Edi,
    string Esi,
    string Ebp,
    string OriginalEsp,
    string Ebx,
    string Edx,
    string Ecx,
    string Eax
);
