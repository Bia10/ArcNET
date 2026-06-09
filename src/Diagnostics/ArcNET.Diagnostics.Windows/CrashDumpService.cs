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

    private static MiniDumpType ToMiniDumpType(CrashDumpKind dumpKind) =>
        dumpKind == CrashDumpKind.Full ? DefaultFullDumpType : DefaultMiniDumpType;

    private static int ToWerDumpTypeValue(CrashDumpKind dumpKind) => dumpKind == CrashDumpKind.Full ? 2 : 1;

    private static CrashDumpKind FromWerDumpTypeValue(int dumpType) =>
        dumpType == 2 ? CrashDumpKind.Full : CrashDumpKind.Mini;

    private static void ValidateDumpCount(int dumpCount)
    {
        if (dumpCount <= 0)
            throw new InvalidOperationException("Dump count must be greater than zero.");
    }
}
