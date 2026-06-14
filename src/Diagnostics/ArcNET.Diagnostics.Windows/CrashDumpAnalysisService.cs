using System.Diagnostics;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public static class CrashDumpAnalysisService
{
    public static async Task<CrashDumpAnalysisSnapshot> AnalyzeDumpAsync(
        string dumpPath,
        string? modulePath = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dumpPath);

        var fullDumpPath = Path.GetFullPath(dumpPath);
        if (!File.Exists(fullDumpPath))
            throw new FileNotFoundException($"Dump '{fullDumpPath}' does not exist.", fullDumpPath);

        if (!TryFindAnalyzerPath(out var analyzerPath))
        {
            return new CrashDumpAnalysisSnapshot(
                DateTimeOffset.UtcNow,
                fullDumpPath,
                AnalyzerFound: false,
                AnalysisWritten: false,
                AnalyzerPath: string.Empty,
                OutputPath: string.Empty,
                ExitCode: null,
                "Crash-dump analysis skipped",
                [
                    "Install Debugging Tools for Windows so ArcNET can auto-print a native stack trace.",
                    "ArcNET looks for cdb.exe in PATH or in the standard Windows Kits debugger folders.",
                ],
                ProcessName: null,
                ExceptionCode: null,
                FaultingInstruction: null,
                StackPreview: []
            );
        }

        var outputPath = $"{fullDumpPath}.stacktrace.txt";
        var startInfo = new ProcessStartInfo(analyzerPath)
        {
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("-z");
        startInfo.ArgumentList.Add(fullDumpPath);
        startInfo.ArgumentList.Add("-lines");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(CreateCommandScript(modulePath));

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException($"Unable to start dump analyzer '{analyzerPath}'.");

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(DefaultAnalysisTimeout);

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryTerminate(process);
            var timedOutOutput = await BuildCombinedOutputAsync(standardOutputTask, standardErrorTask);
            var timeoutText = CreateTimeoutOutput(analyzerPath, fullDumpPath, timedOutOutput);
            await File.WriteAllTextAsync(outputPath, timeoutText, cancellationToken);
            var parsedTimeoutOutput = CrashDumpAnalysisParser.Parse(timedOutOutput);
            return new CrashDumpAnalysisSnapshot(
                DateTimeOffset.UtcNow,
                fullDumpPath,
                AnalyzerFound: true,
                AnalysisWritten: true,
                analyzerPath,
                outputPath,
                ExitCode: null,
                "Crash-dump analysis timed out",
                [
                    $"Partial output was written to {outputPath}.",
                    $"cdb.exe did not finish within {DefaultAnalysisTimeout.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)} seconds.",
                ],
                parsedTimeoutOutput.ProcessName,
                parsedTimeoutOutput.ExceptionCode,
                parsedTimeoutOutput.FaultingInstruction,
                parsedTimeoutOutput.StackPreview
            );
        }

        var combinedOutput = await BuildCombinedOutputAsync(standardOutputTask, standardErrorTask);
        var exitCode = process.ExitCode;
        await File.WriteAllTextAsync(outputPath, combinedOutput, cancellationToken);
        var parsedOutput = CrashDumpAnalysisParser.Parse(combinedOutput);
        return new CrashDumpAnalysisSnapshot(
            DateTimeOffset.UtcNow,
            fullDumpPath,
            AnalyzerFound: true,
            AnalysisWritten: true,
            analyzerPath,
            outputPath,
            exitCode,
            exitCode == 0 ? "Crash-dump analysis captured" : $"Crash-dump analysis exited with code {exitCode}",
            parsedOutput.Highlights,
            parsedOutput.ProcessName,
            parsedOutput.ExceptionCode,
            parsedOutput.FaultingInstruction,
            parsedOutput.StackPreview
        );
    }

    private static async Task<string> BuildCombinedOutputAsync(
        Task<string> standardOutputTask,
        Task<string> standardErrorTask
    )
    {
        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;
        return string.IsNullOrWhiteSpace(standardError)
            ? standardOutput
            : $"{standardOutput}{Environment.NewLine}{Environment.NewLine}[stderr]{Environment.NewLine}{standardError}";
    }

    private static string CreateCommandScript(string? modulePath)
    {
        List<string> commands = [".symfix"];
        var moduleDirectory = string.IsNullOrWhiteSpace(modulePath)
            ? null
            : Path.GetDirectoryName(Path.GetFullPath(modulePath));
        if (!string.IsNullOrWhiteSpace(moduleDirectory))
        {
            var quotedDirectory = QuoteDebuggerPath(moduleDirectory);
            commands.Add($".exepath {quotedDirectory}");
            commands.Add($".sympath+ {quotedDirectory}");
        }

        commands.Add(".reload /f");
        commands.Add("!analyze -v");
        commands.Add("~*kb");
        commands.Add("q");
        return string.Join(';', commands);
    }

    private static string CreateTimeoutOutput(string analyzerPath, string dumpPath, string partialOutput) =>
        string.Join(
            Environment.NewLine,
            [
                $"Analyzer: {analyzerPath}",
                $"Dump: {dumpPath}",
                $"Status: timed out after {DefaultAnalysisTimeout.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)} seconds",
                string.Empty,
                partialOutput,
            ]
        );

    private static string QuoteDebuggerPath(string path) => $"\"{path.Replace("\"", "\"\"")}\"";

    private static bool TryFindAnalyzerPath(out string analyzerPath)
    {
        analyzerPath = EnumerateAnalyzerPaths().FirstOrDefault(File.Exists) ?? string.Empty;
        return analyzerPath.Length != 0;
    }

    private static IEnumerable<string> EnumerateAnalyzerPaths()
    {
        foreach (var variableName in AnalyzerOverrideVariables)
        {
            var overriddenPath = Environment.GetEnvironmentVariable(variableName);
            if (!string.IsNullOrWhiteSpace(overriddenPath))
                yield return Path.GetFullPath(overriddenPath);
        }

        foreach (var pathDirectory in EnumeratePathDirectories())
            yield return Path.Combine(pathDirectory, "cdb.exe");

        foreach (var baseDirectory in EnumerateProgramFilesRoots())
        {
            yield return Path.Combine(baseDirectory, "Windows Kits", "10", "Debuggers", "x64", "cdb.exe");
            yield return Path.Combine(baseDirectory, "Windows Kits", "10", "Debuggers", "x86", "cdb.exe");
        }
    }

    private static IEnumerable<string> EnumeratePathDirectories() =>
        (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static directory => directory.Length != 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<string> EnumerateProgramFilesRoots() =>
        new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        }
            .Where(static directory => directory.Length != 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch { }
    }

    private static readonly string[] AnalyzerOverrideVariables = ["ARCNET_CDB_PATH", "CDB_PATH"];

    private static readonly TimeSpan DefaultAnalysisTimeout = TimeSpan.FromSeconds(20);
}
