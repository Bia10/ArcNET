namespace ArcNET.Diagnostics;

public enum InterceptDereferenceSourceKind
{
    Eax = 0,
    Ecx = 1,
    Edx = 2,
    Ebx = 3,
    Esi = 4,
    Edi = 5,
    Ebp = 6,
    OriginalEsp = 7,
    StackIndex = 8,
}
