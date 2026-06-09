namespace ArcNET.Diagnostics;

public sealed record class InterceptRegisterOverrideRequest(
    uint? Edi,
    uint? Esi,
    uint? Ebp,
    uint? Ebx,
    uint? Edx,
    uint? Ecx,
    uint? Eax
);
