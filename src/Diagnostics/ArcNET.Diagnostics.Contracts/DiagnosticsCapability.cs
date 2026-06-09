namespace ArcNET.Diagnostics.Contracts;

[Flags]
public enum DiagnosticsCapability : ushort
{
    None = 0,
    ReadMemory = 1 << 0,
    CaptureDump = 1 << 1,
    LoadModuleSymbols = 1 << 2,
    ResolveRuntimeProfile = 1 << 3,
    ReadStructuredState = 1 << 4,
    WatchHooks = 1 << 5,
    DecodeObjectLayout = 1 << 6,
    InvokeFunctions = 1 << 7,
    InterceptFunctions = 1 << 8,
    MutateRuntime = 1 << 9,
}
