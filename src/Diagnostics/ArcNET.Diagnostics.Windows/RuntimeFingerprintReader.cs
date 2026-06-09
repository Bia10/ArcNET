using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

public static class RuntimeFingerprintReader
{
    public static RuntimeFingerprint Create(
        string processName,
        int processId,
        string modulePath,
        nint moduleBase,
        int moduleSize
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processName);
        ArgumentException.ThrowIfNullOrWhiteSpace(modulePath);

        var fileInfo = new FileInfo(modulePath);
        var moduleFileName = Path.GetFileName(modulePath);
        var runtimeKind = RuntimeProfileMatcher.ClassifyRuntimeKind(moduleFileName, processName);

        return new RuntimeFingerprint(
            processName,
            processId,
            runtimeKind,
            moduleFileName,
            modulePath,
            FormatAddress(moduleBase),
            moduleSize,
            fileInfo.Exists ? fileInfo.Length : 0,
            fileInfo.Exists ? fileInfo.LastWriteTimeUtc : DateTime.MinValue
        );
    }

    private static string FormatAddress(nint address)
    {
        var value = (ulong)(long)address;
        return value <= uint.MaxValue ? $"0x{value:X8}" : $"0x{value:X16}";
    }
}
