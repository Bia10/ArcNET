using System.Diagnostics;

namespace ArcNET.Diagnostics.Windows;

public static class ProcessCatalog
{
    public static IReadOnlyList<string> DefaultProcessNames => s_defaultProcessNames;

    public static IReadOnlyList<string> GetRequestedProcessNames(string? overrideProcessName = null)
    {
        if (string.IsNullOrWhiteSpace(overrideProcessName))
            return s_defaultProcessNames;

        return [NormalizeProcessName(overrideProcessName)];
    }

    public static string DescribeTargets(IEnumerable<string> processNames) =>
        string.Join(" / ", processNames.Select(static name => $"{Path.GetFileNameWithoutExtension(name)}.exe"));

    public static IReadOnlyList<string> GetRunningProcessNames(IEnumerable<string> processNames)
    {
        ArgumentNullException.ThrowIfNull(processNames);

        List<string> running = [];
        foreach (
            var processName in processNames.Select(NormalizeProcessName).Distinct(StringComparer.OrdinalIgnoreCase)
        )
        {
            if (HasRunningProcess(processName))
                running.Add(processName);
        }

        return running;
    }

    public static IReadOnlyList<RunningProcessInfo> GetRunningProcesses(IEnumerable<string> processNames)
    {
        ArgumentNullException.ThrowIfNull(processNames);

        List<RunningProcessInfo> running = [];
        foreach (
            var processName in processNames.Select(NormalizeProcessName).Distinct(StringComparer.OrdinalIgnoreCase)
        )
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                try
                {
                    var module = process.MainModule;
                    if (module is null)
                        continue;

                    running.Add(
                        new RunningProcessInfo(
                            process.ProcessName,
                            process.Id,
                            module.ModuleName,
                            module.FileName,
                            module.BaseAddress,
                            module.ModuleMemorySize
                        )
                    );
                }
                catch
                {
                    // Skip processes whose module metadata is not accessible from this session.
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        return
        [
            .. running
                .OrderBy(static process => process.ProcessName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static process => process.ProcessId),
        ];
    }

    public static bool TrySelectSingleRunningProcess(
        IEnumerable<string> processNames,
        out string selectedProcessName,
        out string error
    )
    {
        ArgumentNullException.ThrowIfNull(processNames);

        var requested = processNames.Select(NormalizeProcessName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var running = GetRunningProcessNames(requested);
        if (running.Count == 0)
        {
            selectedProcessName = string.Empty;
            error = $"None of the supported processes are running ({DescribeTargets(requested)}).";
            return false;
        }

        if (running.Count > 1)
        {
            selectedProcessName = string.Empty;
            error =
                $"Multiple supported processes are running ({string.Join(", ", running.Select(static name => $"{name}.exe"))}).";
            return false;
        }

        selectedProcessName = running[0];
        error = string.Empty;
        return true;
    }

    public static string NormalizeProcessName(string processName)
    {
        var normalized = Path.GetFileNameWithoutExtension(processName.Trim());
        if (normalized.Length == 0)
            throw new InvalidOperationException("Process name must not be empty.");

        return normalized;
    }

    private static bool HasRunningProcess(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (var process in processes)
                process.Dispose();
        }
    }

    private static readonly string[] s_defaultProcessNames = ["Arcanum", "arcanum-ce"];
}
