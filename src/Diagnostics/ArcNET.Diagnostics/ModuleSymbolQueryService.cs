namespace ArcNET.Diagnostics;

public sealed class ModuleSymbolQueryService(IRuntimePlatformService platformService)
{
    public ModuleSymbolQuerySnapshot QueryLive(int processId, ModuleSymbolQueryRequest request)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(request);
        return platformService.QueryRuntimeModuleSymbols(processId, request);
    }

    public ModuleSymbolQuerySnapshot QueryFile(string modulePath, ModuleSymbolQueryRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modulePath);
        ArgumentNullException.ThrowIfNull(request);
        return platformService.QueryFileModuleSymbols(modulePath, request);
    }
}
