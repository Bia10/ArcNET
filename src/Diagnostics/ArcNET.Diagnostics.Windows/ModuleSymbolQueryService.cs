using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

public static class ModuleSymbolQueryService
{
    public static ModuleSymbolQuerySnapshot QueryFile(string modulePath, ModuleSymbolQueryRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modulePath);
        ArgumentNullException.ThrowIfNull(request);

        var catalog = ModuleSymbolCatalogLoader.Load(modulePath);
        return CreateSnapshot(
            catalog,
            request,
            modulePath: Path.GetFullPath(modulePath),
            moduleBase: null,
            moduleBaseAddress: null,
            fingerprint: null
        );
    }

    public static ModuleSymbolQuerySnapshot QueryLive(ProcessMemory memory, ModuleSymbolQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(request);

        var catalog = ModuleSymbolCatalogLoader.Load(memory.ModulePath);
        var fingerprint = RuntimeFingerprintReader.Create(
            memory.ProcessName,
            memory.ProcessId,
            memory.ModulePath,
            memory.ModuleBase,
            memory.ModuleSize
        );
        return CreateSnapshot(
            catalog,
            request,
            memory.ModulePath,
            ProcessMemory.FormatAddress(memory.ModuleBase),
            memory.ToUInt32Address(memory.ModuleBase),
            fingerprint
        );
    }

    private static ModuleSymbolQuerySnapshot CreateSnapshot(
        ModuleSymbolCatalog catalog,
        ModuleSymbolQueryRequest request,
        string modulePath,
        string? moduleBase,
        uint? moduleBaseAddress,
        RuntimeFingerprint? fingerprint
    )
    {
        var limit =
            request.Limit <= 0
                ? throw new InvalidOperationException("Limit must be greater than zero.")
                : request.Limit;
        var symbols = catalog
            .Query(request.Filter, limit, request.DuplicatesOnly)
            .Select(symbol => new ModuleSymbolEntrySnapshot(
                symbol.Name,
                $"{symbol.Name} ({CodeCatalog.FormatModuleAddress(catalog.ModuleFileName, symbol.Rva)})",
                symbol.Rva,
                $"0x{symbol.Rva:X8}",
                moduleBaseAddress.HasValue
                    ? ProcessMemory.FormatAddress((nint)(long)(moduleBaseAddress.Value + symbol.Rva))
                    : null,
                symbol.Size,
                $"0x{symbol.Size:X}",
                catalog.Symbols.Count(candidate =>
                    candidate.Name.Equals(symbol.Name, StringComparison.OrdinalIgnoreCase)
                )
            ))
            .ToArray();

        return new ModuleSymbolQuerySnapshot(
            DateTimeOffset.UtcNow,
            modulePath,
            catalog.ModuleFileName,
            moduleBase,
            fingerprint,
            request.Filter,
            limit,
            request.DuplicatesOnly,
            catalog.FunctionCount,
            catalog.UniqueNameCount,
            catalog.DuplicateNameCount,
            symbols
        );
    }
}
