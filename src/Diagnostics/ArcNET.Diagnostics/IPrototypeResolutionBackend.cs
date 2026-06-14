using ArcNET.Diagnostics.Contracts;
using ArcNET.GameData.Workspace;

namespace ArcNET.Diagnostics;

public interface IPrototypeResolutionBackend : IHandleBackend
{
    Task<IReadOnlyList<PrototypePaletteEntry>> LoadPaletteAsync(string workspacePath);

    Task<IReadOnlyList<StaticObjectCatalogEntry>> LoadStaticObjectCatalogAsync(string workspacePath);

    PrototypeHandleResolutionResult ResolvePrototypeHandle(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        int protoNumber
    );
}
