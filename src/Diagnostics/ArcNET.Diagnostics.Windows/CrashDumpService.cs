using System.ComponentModel;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using Microsoft.Win32;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public static class CrashDumpService
{
    private const MiniDumpType DefaultMiniDumpType =
        MiniDumpType.Normal
        | MiniDumpType.WithDataSegs
        | MiniDumpType.WithHandleData
        | MiniDumpType.WithThreadInfo
        | MiniDumpType.WithUnloadedModules;

    private const MiniDumpType DefaultFullDumpType =
        MiniDumpType.WithFullMemory
        | MiniDumpType.WithDataSegs
        | MiniDumpType.WithHandleData
        | MiniDumpType.WithIndirectlyReferencedMemory
        | MiniDumpType.WithThreadInfo
        | MiniDumpType.WithUnloadedModules;

    private const string WerLocalDumpsRegistryPath = @"Software\Microsoft\Windows\Windows Error Reporting\LocalDumps";

    public static CrashDumpWriteSnapshot WriteDump(
        ProcessMemory memory,
        string outputPath,
        CrashDumpKind dumpKind = CrashDumpKind.Mini
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var fullOutputPath = Path.GetFullPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(fullOutputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new InvalidOperationException("The dump path must include a directory.");

        Directory.CreateDirectory(outputDirectory);

        using var stream = new FileStream(fullOutputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        if (
            !DbgHelpNativeMethods.MiniDumpWriteDump(
                memory.Handle,
                memory.ProcessId,
                stream.SafeFileHandle.DangerousGetHandle(),
                ToMiniDumpType(dumpKind),
                0,
                0,
                0
            )
        )
        {
            var errorCode = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            throw new Win32Exception(
                errorCode,
                $"Failed to write process dump to '{fullOutputPath}' (Win32 {errorCode})."
            );
        }

        return new CrashDumpWriteSnapshot(
            DateTimeOffset.UtcNow,
            memory.ProcessId,
            memory.ProcessName,
            memory.ModulePath,
            ProcessMemory.FormatAddress(memory.ModuleBase),
            fullOutputPath,
            dumpKind
        );
    }

    public static CrashDumpAutoConfigurationSnapshot EnableAutomaticDumps(
        string dumpDirectory,
        CrashDumpKind dumpKind = CrashDumpKind.Mini,
        int dumpCount = 5,
        string processExecutableName = "Arcanum.exe"
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dumpDirectory);
        ValidateDumpCount(dumpCount);

        var fullOutputDirectory = Path.GetFullPath(dumpDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        using var key =
            Registry.CurrentUser.CreateSubKey($@"{WerLocalDumpsRegistryPath}\{processExecutableName}", writable: true)
            ?? throw new InvalidOperationException("Unable to create the LocalDumps registry key.");
        key.SetValue("DumpFolder", fullOutputDirectory, RegistryValueKind.ExpandString);
        key.SetValue("DumpType", ToWerDumpTypeValue(dumpKind), RegistryValueKind.DWord);
        key.SetValue("DumpCount", dumpCount, RegistryValueKind.DWord);

        return new CrashDumpAutoConfigurationSnapshot(
            true,
            processExecutableName,
            "CurrentUser",
            $@"HKCU\{WerLocalDumpsRegistryPath}\{processExecutableName}",
            fullOutputDirectory,
            dumpKind,
            dumpCount
        );
    }

    public static CrashDumpAutoConfigurationSnapshot DisableAutomaticDumps(string processExecutableName = "Arcanum.exe")
    {
        using var parent = Registry.CurrentUser.OpenSubKey(WerLocalDumpsRegistryPath, writable: true);
        parent?.DeleteSubKeyTree(processExecutableName, throwOnMissingSubKey: false);

        return new CrashDumpAutoConfigurationSnapshot(
            false,
            processExecutableName,
            "CurrentUser",
            $@"HKCU\{WerLocalDumpsRegistryPath}\{processExecutableName}",
            null,
            null,
            null
        );
    }

    public static CrashDumpAutoConfigurationSnapshot GetAutomaticDumpStatus(
        string processExecutableName = "Arcanum.exe"
    )
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            $@"{WerLocalDumpsRegistryPath}\{processExecutableName}",
            writable: false
        );
        if (key is null)
        {
            return new CrashDumpAutoConfigurationSnapshot(
                false,
                processExecutableName,
                "CurrentUser",
                $@"HKCU\{WerLocalDumpsRegistryPath}\{processExecutableName}",
                null,
                null,
                null
            );
        }

        var dumpFolder = key.GetValue("DumpFolder") as string;
        var dumpType = key.GetValue("DumpType") is int rawType ? FromWerDumpTypeValue(rawType) : CrashDumpKind.Mini;
        var dumpCount = key.GetValue("DumpCount") is int rawCount ? rawCount : 10;
        return new CrashDumpAutoConfigurationSnapshot(
            true,
            processExecutableName,
            "CurrentUser",
            $@"HKCU\{WerLocalDumpsRegistryPath}\{processExecutableName}",
            dumpFolder,
            dumpType,
            dumpCount
        );
    }

    public static async Task<CrashDumpAutoInspectionSnapshot> InspectAutomaticDumpsAsync(
        string processExecutableName = "Arcanum.exe",
        string? modulePath = null,
        CancellationToken cancellationToken = default
    )
    {
        var configuration = GetAutomaticDumpStatus(processExecutableName);
        if (!configuration.IsEnabled)
        {
            return new CrashDumpAutoInspectionSnapshot(
                DateTimeOffset.UtcNow,
                configuration,
                "Automatic dumps are disabled",
                ["Enable LocalDumps before expecting crash dumps or stack traces."],
                LatestDumpPath: null,
                LatestDumpWrittenAtUtc: null,
                LatestDumpSizeBytes: null,
                Analysis: null
            );
        }

        if (string.IsNullOrWhiteSpace(configuration.DumpFolder))
        {
            return new CrashDumpAutoInspectionSnapshot(
                DateTimeOffset.UtcNow,
                configuration,
                "Automatic dump folder unavailable",
                ["The LocalDumps registry entry is enabled, but no dump folder is configured."],
                LatestDumpPath: null,
                LatestDumpWrittenAtUtc: null,
                LatestDumpSizeBytes: null,
                Analysis: null
            );
        }

        var dumpDirectory = Path.GetFullPath(configuration.DumpFolder);
        if (!Directory.Exists(dumpDirectory))
        {
            return new CrashDumpAutoInspectionSnapshot(
                DateTimeOffset.UtcNow,
                configuration with
                {
                    DumpFolder = dumpDirectory,
                },
                "Automatic dump folder missing",
                [$"Configured dump folder '{dumpDirectory}' does not exist yet."],
                LatestDumpPath: null,
                LatestDumpWrittenAtUtc: null,
                LatestDumpSizeBytes: null,
                Analysis: null
            );
        }

        var latestDump = FindLatestDump(dumpDirectory, processExecutableName, out var usedFallbackSearch);
        if (latestDump is null)
        {
            return new CrashDumpAutoInspectionSnapshot(
                DateTimeOffset.UtcNow,
                configuration with
                {
                    DumpFolder = dumpDirectory,
                },
                "Automatic dumps are enabled",
                ["No crash dumps were found in the configured LocalDumps folder yet."],
                LatestDumpPath: null,
                LatestDumpWrittenAtUtc: null,
                LatestDumpSizeBytes: null,
                Analysis: null
            );
        }

        List<string> notes = [];
        if (usedFallbackSearch)
        {
            notes.Add(
                $"No dump file matched '{processExecutableName}' by name, so ArcNET inspected the newest dump in '{dumpDirectory}'."
            );
        }

        var analysis = await CrashDumpAnalysisService.AnalyzeDumpAsync(
            latestDump.FullName,
            modulePath,
            cancellationToken
        );
        if (!string.IsNullOrWhiteSpace(analysis.OutputPath))
            notes.Add($"Analysis output: {analysis.OutputPath}");

        return new CrashDumpAutoInspectionSnapshot(
            DateTimeOffset.UtcNow,
            configuration with
            {
                DumpFolder = dumpDirectory,
            },
            analysis.AnalyzerFound ? "Latest automatic dump analyzed" : "Latest automatic dump found",
            notes,
            latestDump.FullName,
            latestDump.LastWriteTimeUtc,
            latestDump.Length,
            analysis
        );
    }

    private static MiniDumpType ToMiniDumpType(CrashDumpKind dumpKind) =>
        dumpKind == CrashDumpKind.Full ? DefaultFullDumpType : DefaultMiniDumpType;

    private static int ToWerDumpTypeValue(CrashDumpKind dumpKind) => dumpKind == CrashDumpKind.Full ? 2 : 1;

    private static CrashDumpKind FromWerDumpTypeValue(int dumpType) =>
        dumpType == 2 ? CrashDumpKind.Full : CrashDumpKind.Mini;

    private static FileInfo? FindLatestDump(
        string dumpDirectory,
        string processExecutableName,
        out bool usedFallbackSearch
    )
    {
        var dumpFiles = Directory
            .EnumerateFiles(dumpDirectory, "*.dmp", SearchOption.TopDirectoryOnly)
            .Select(static path => new FileInfo(path))
            .OrderByDescending(static file => file.LastWriteTimeUtc)
            .ToArray();
        if (dumpFiles.Length == 0)
        {
            usedFallbackSearch = false;
            return null;
        }

        var nameTokens = new[] { processExecutableName, Path.GetFileNameWithoutExtension(processExecutableName) }
            .Where(static token => !string.IsNullOrWhiteSpace(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var exactMatch = dumpFiles.FirstOrDefault(file =>
            nameTokens.Any(token => file.Name.StartsWith(token, StringComparison.OrdinalIgnoreCase))
        );
        if (exactMatch is not null)
        {
            usedFallbackSearch = false;
            return exactMatch;
        }

        usedFallbackSearch = true;
        return dumpFiles[0];
    }

    private static void ValidateDumpCount(int dumpCount)
    {
        if (dumpCount <= 0)
            throw new InvalidOperationException("Dump count must be greater than zero.");
    }
}
