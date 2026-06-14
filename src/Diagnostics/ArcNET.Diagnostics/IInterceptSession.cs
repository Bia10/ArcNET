namespace ArcNET.Diagnostics;

public interface IInterceptSession : IDisposable
{
    bool HasExited { get; }

    string ModuleFileName { get; }

    RuntimeInterceptionReadResult ReadSince(uint lastSequence);

    InterceptMemoryReadResult ReadMemory(uint address, int requestedByteCount);
}
