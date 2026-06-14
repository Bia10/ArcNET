namespace ArcNET.Diagnostics;

public sealed class CrashDumpService(IRuntimePlatformService platformService)
{
    public Task<CrashDumpCaptureSnapshot> WriteDumpAsync(
        int processId,
        string outputPath,
        CrashDumpKind dumpKind = CrashDumpKind.Mini,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return platformService.WriteRuntimeCrashDumpAsync(processId, outputPath, dumpKind, cancellationToken);
    }

    public Task<CrashDumpAutoInspectionSnapshot> InspectAutomaticDumpsAsync(
        string processExecutableName = "Arcanum.exe",
        string? modulePath = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processExecutableName);
        return platformService.InspectAutomaticCrashDumpsAsync(processExecutableName, modulePath, cancellationToken);
    }

    public CrashDumpAutoConfigurationSnapshot EnableAutomaticDumps(
        string dumpDirectory,
        CrashDumpKind dumpKind = CrashDumpKind.Mini,
        int dumpCount = 5,
        string processExecutableName = "Arcanum.exe"
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dumpDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(processExecutableName);
        return platformService.EnableAutomaticDumps(dumpDirectory, dumpKind, dumpCount, processExecutableName);
    }

    public CrashDumpAutoConfigurationSnapshot DisableAutomaticDumps(string processExecutableName = "Arcanum.exe")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processExecutableName);
        return platformService.DisableAutomaticDumps(processExecutableName);
    }
}
