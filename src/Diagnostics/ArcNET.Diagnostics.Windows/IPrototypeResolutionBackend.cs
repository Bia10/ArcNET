using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

public interface IPrototypeResolutionBackend : IHandleBackend
{
    IReadOnlyList<PrototypePaletteEntry> LoadPalette(string modulePath);

    PrototypeHandleResolutionResult ResolvePrototypeHandle(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        int protoNumber
    );
}
