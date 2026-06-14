namespace ArcNET.Diagnostics;

public interface IRuntimePlatformService
{
    RuntimeStatusSnapshot InspectRuntimeStatus(int processId);

    RuntimeActionPointsWriteSnapshot WriteRuntimeActionPoints(int processId, int value);

    ModuleSymbolQuerySnapshot QueryRuntimeModuleSymbols(int processId, ModuleSymbolQueryRequest request);

    ModuleSymbolQuerySnapshot QueryFileModuleSymbols(string modulePath, ModuleSymbolQueryRequest request);

    Task<CrashDumpCaptureSnapshot> WriteRuntimeCrashDumpAsync(
        int processId,
        string outputPath,
        CrashDumpKind dumpKind = CrashDumpKind.Mini,
        CancellationToken cancellationToken = default
    );

    Task<CrashDumpAnalysisSnapshot> AnalyzeCrashDumpAsync(
        string dumpPath,
        string? modulePath = null,
        CancellationToken cancellationToken = default
    );

    Task<CrashDumpAutoInspectionSnapshot> InspectAutomaticCrashDumpsAsync(
        string processExecutableName = "Arcanum.exe",
        string? modulePath = null,
        CancellationToken cancellationToken = default
    );

    CrashDumpAutoConfigurationSnapshot EnableAutomaticDumps(
        string dumpDirectory,
        CrashDumpKind dumpKind = CrashDumpKind.Mini,
        int dumpCount = 5,
        string processExecutableName = "Arcanum.exe"
    );

    CrashDumpAutoConfigurationSnapshot DisableAutomaticDumps(string processExecutableName = "Arcanum.exe");

    InterceptTarget ResolveInterceptTarget(int processId, string targetText);
}
