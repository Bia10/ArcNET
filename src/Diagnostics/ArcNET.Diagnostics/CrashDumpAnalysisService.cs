namespace ArcNET.Diagnostics;

public sealed class CrashDumpAnalysisService(IRuntimePlatformService platformService)
{
    public Task<CrashDumpAnalysisSnapshot> AnalyzeDumpAsync(
        string dumpPath,
        string? modulePath = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dumpPath);
        return platformService.AnalyzeCrashDumpAsync(dumpPath, modulePath, cancellationToken);
    }
}
