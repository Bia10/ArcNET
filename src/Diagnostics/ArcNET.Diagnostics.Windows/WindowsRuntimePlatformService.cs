using System.Globalization;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsRuntimePlatformService : IRuntimePlatformService
{
    public RuntimeStatusSnapshot InspectRuntimeStatus(int processId)
    {
        using var memory = ProcessMemory.Attach(processId);
        return RuntimeStatusService.Inspect(memory);
    }

    public RuntimeActionPointsWriteSnapshot WriteRuntimeActionPoints(int processId, int value)
    {
        using var memory = ProcessMemory.Attach(processId);
        var mutation = RuntimeStatusService.WriteActionPoints(memory, value);
        var status = RuntimeStatusService.Inspect(memory);
        return new RuntimeActionPointsWriteSnapshot(mutation, status);
    }

    public ModuleSymbolQuerySnapshot QueryRuntimeModuleSymbols(int processId, ModuleSymbolQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var memory = ProcessMemory.Attach(processId);
        return ModuleSymbolQueryService.QueryLive(memory, request);
    }

    public ModuleSymbolQuerySnapshot QueryFileModuleSymbols(string modulePath, ModuleSymbolQueryRequest request) =>
        ModuleSymbolQueryService.QueryFile(modulePath, request);

    public async Task<CrashDumpCaptureSnapshot> WriteRuntimeCrashDumpAsync(
        int processId,
        string outputPath,
        CrashDumpKind dumpKind = CrashDumpKind.Mini,
        CancellationToken cancellationToken = default
    )
    {
        using var memory = ProcessMemory.Attach(processId);
        var dump = CrashDumpService.WriteDump(memory, outputPath, dumpKind);
        var analysis = await CrashDumpAnalysisService.AnalyzeDumpAsync(
            dump.OutputPath,
            memory.ModulePath,
            cancellationToken
        );
        return new CrashDumpCaptureSnapshot(dump, analysis);
    }

    public Task<CrashDumpAnalysisSnapshot> AnalyzeCrashDumpAsync(
        string dumpPath,
        string? modulePath = null,
        CancellationToken cancellationToken = default
    ) => CrashDumpAnalysisService.AnalyzeDumpAsync(dumpPath, modulePath, cancellationToken);

    public Task<CrashDumpAutoInspectionSnapshot> InspectAutomaticCrashDumpsAsync(
        string processExecutableName = "Arcanum.exe",
        string? modulePath = null,
        CancellationToken cancellationToken = default
    ) => CrashDumpService.InspectAutomaticDumpsAsync(processExecutableName, modulePath, cancellationToken);

    public CrashDumpAutoConfigurationSnapshot EnableAutomaticDumps(
        string dumpDirectory,
        CrashDumpKind dumpKind = CrashDumpKind.Mini,
        int dumpCount = 5,
        string processExecutableName = "Arcanum.exe"
    ) => CrashDumpService.EnableAutomaticDumps(dumpDirectory, dumpKind, dumpCount, processExecutableName);

    public CrashDumpAutoConfigurationSnapshot DisableAutomaticDumps(string processExecutableName = "Arcanum.exe") =>
        CrashDumpService.DisableAutomaticDumps(processExecutableName);

    public InterceptTarget ResolveInterceptTarget(int processId, string targetText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetText);

        using var memory = ProcessMemory.Attach(processId);
        var trimmed = targetText.Trim();
        if (FunctionCatalog.TryGetDefinition(trimmed, out var definition))
        {
            return new InterceptTarget(
                definition.Key,
                checked((uint)(long)memory.ResolveRva(definition.Rva)),
                unchecked((uint)definition.Rva),
                definition.Site,
                definition.Summary,
                "catalog-function"
            );
        }

        if (!TryParseRvaValue(trimmed, out var rva))
            throw new FormatException($"Unknown interception target: {targetText}");

        return new InterceptTarget(
            $"raw_rva_{rva:X8}",
            checked((uint)(long)memory.ResolveRva(unchecked((int)rva))),
            rva,
            CodeCatalog.FormatModuleAddress(Path.GetFileName(memory.ModulePath), rva),
            "Raw interception target.",
            "raw-rva"
        );
    }

    private static bool TryParseRvaValue(string text, out uint value)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(trimmed[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
        }

        return uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
