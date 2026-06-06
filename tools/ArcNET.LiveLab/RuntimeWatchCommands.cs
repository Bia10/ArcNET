using System.ComponentModel;
using System.Runtime.Versioning;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal static class RuntimeWatchCommands
{
    public static async Task<int> RunWatchAsync(string[] args)
    {
        if (args.Length == 0)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab watch <progression|inventory|npcs|critters|all|hook-name> [selector...] [--duration-ms <ms>] [--poll-ms <ms>] [--summary-ms <ms>] [--console-events] [--include-noise] [--wait-for-process] [--attach-timeout-ms <ms>] [--log-file <path>]"
            );
        }

        if (args.Any(static arg => arg.Equals("list", StringComparison.OrdinalIgnoreCase) || arg == "--list"))
        {
            LiveLabCli.WriteJson(RuntimeWatchCatalog.DescribeCatalog());
            return 0;
        }

        var options = ParseOptions(args);
        var hooks = RuntimeWatchCatalog.ResolveSelectors(options.Selectors);
        if (hooks.Count == 0)
            throw new InvalidOperationException("No watch hooks were selected.");

        using var memory = await AttachForWatchAsync(options);
        return await WatchAsync(memory, options, hooks);
    }

    private static async Task<int> WatchAsync(
        ProcessMemory memory,
        WatchOptions options,
        IReadOnlyList<RuntimeWatchHookDefinition> hooks
    )
    {
        Console.WriteLine("preparing live object name resolution...");
        using var resolver = await RuntimeWatchObjectResolver.CreateAsync(memory);
        using var logSession = RuntimeWatchLogSession.Create(options.LogFilePath, resolver);
        using var session = RuntimeWatchSession.Install(memory, hooks);
        using var cancellation = options.Duration is { } duration
            ? new CancellationTokenSource(duration)
            : new CancellationTokenSource();
        var consoleReporter = new RuntimeWatchConsoleReporter(options.ConsoleEvents, options.SummaryInterval, resolver);
        var lastSequence = 0u;
        var emittedEvents = 0;
        var suppressedEvents = 0;
        var droppedEvents = 0;
        var inconsistentRecords = 0;
        var targetExited = false;

        ConsoleCancelEventHandler? handler = null;
        handler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        Console.CancelKeyPress += handler;
        try
        {
            logSession.LogWatchStart(
                options.Selectors,
                options.PollInterval,
                options.Duration,
                options.ConsoleEvents,
                options.IncludeNoise,
                options.SummaryInterval,
                memory,
                hooks
            );
            consoleReporter.WriteStart(options.Selectors, hooks, logSession.LogFilePath, options.IncludeNoise, memory);

            while (!cancellation.IsCancellationRequested)
            {
                if (
                    !TryEmitPoll(
                        memory,
                        logSession,
                        consoleReporter,
                        options.IncludeNoise,
                        session,
                        ref lastSequence,
                        ref emittedEvents,
                        ref suppressedEvents,
                        ref droppedEvents,
                        ref inconsistentRecords
                    )
                )
                {
                    targetExited = true;
                    break;
                }

                consoleReporter.MaybeWriteSummary();

                await Task.Delay(options.PollInterval, cancellation.Token);
            }

            if (!targetExited)
            {
                _ = TryEmitPoll(
                    memory,
                    logSession,
                    consoleReporter,
                    options.IncludeNoise,
                    session,
                    ref lastSequence,
                    ref emittedEvents,
                    ref suppressedEvents,
                    ref droppedEvents,
                    ref inconsistentRecords
                );
            }

            consoleReporter.MaybeWriteSummary();

            logSession.LogWatchEnd(lastSequence, emittedEvents, suppressedEvents, droppedEvents, inconsistentRecords);
            consoleReporter.WriteEnd(logSession.LogFilePath);
            return 0;
        }
        catch (OperationCanceledException) { }
        finally
        {
            Console.CancelKeyPress -= handler;
        }

        if (!targetExited)
        {
            _ = TryEmitPoll(
                memory,
                logSession,
                consoleReporter,
                options.IncludeNoise,
                session,
                ref lastSequence,
                ref emittedEvents,
                ref suppressedEvents,
                ref droppedEvents,
                ref inconsistentRecords
            );
        }

        logSession.LogWatchEnd(lastSequence, emittedEvents, suppressedEvents, droppedEvents, inconsistentRecords);
        consoleReporter.WriteEnd(logSession.LogFilePath);
        return 0;
    }

    private static bool TryEmitPoll(
        ProcessMemory memory,
        RuntimeWatchLogSession logSession,
        RuntimeWatchConsoleReporter consoleReporter,
        bool includeNoise,
        RuntimeWatchSession session,
        ref uint lastSequence,
        ref int emittedEvents,
        ref int suppressedEvents,
        ref int droppedEvents,
        ref int inconsistentRecords
    )
    {
        try
        {
            EmitPoll(
                logSession,
                consoleReporter,
                includeNoise,
                session.ReadSince(lastSequence),
                ref lastSequence,
                ref emittedEvents,
                ref suppressedEvents,
                ref droppedEvents,
                ref inconsistentRecords
            );
            return true;
        }
        catch (Win32Exception) when (memory.HasExited)
        {
            consoleReporter.WriteTargetExited();
            return false;
        }
    }

    private static void EmitPoll(
        RuntimeWatchLogSession logSession,
        RuntimeWatchConsoleReporter consoleReporter,
        bool includeNoise,
        RuntimeWatchSession.RuntimeWatchReadResult poll,
        ref uint lastSequence,
        ref int emittedEvents,
        ref int suppressedEvents,
        ref int droppedEvents,
        ref int inconsistentRecords
    )
    {
        lastSequence = poll.WriteSequence;
        droppedEvents += poll.DroppedEvents;
        inconsistentRecords += poll.InconsistentRecords;

        if (poll.DroppedEvents > 0)
        {
            logSession.LogWatchOverflow(poll.DroppedEvents, poll.WriteSequence);
            consoleReporter.RecordOverflow(poll.DroppedEvents, poll.WriteSequence);
        }

        if (poll.InconsistentRecords > 0)
        {
            logSession.LogWatchWarning(poll.InconsistentRecords, poll.WriteSequence);
            consoleReporter.RecordWarning(poll.InconsistentRecords, poll.WriteSequence);
        }

        foreach (var capturedEvent in poll.Events)
        {
            if (!includeNoise && RuntimeWatchEventInterpreter.IsNoise(capturedEvent))
            {
                suppressedEvents++;
                consoleReporter.RecordSuppressed(capturedEvent);
                continue;
            }

            emittedEvents++;
            logSession.LogEvent(capturedEvent, DateTime.UtcNow);
            consoleReporter.RecordEvent(capturedEvent);
        }
    }

    private static WatchOptions ParseOptions(string[] args)
    {
        List<string> selectors = [];
        TimeSpan? attachTimeout = null;
        TimeSpan? duration = null;
        string? logFilePath = null;
        var consoleEvents = false;
        var includeNoise = false;
        var pollInterval = TimeSpan.FromMilliseconds(100);
        var summaryInterval = TimeSpan.FromSeconds(5);
        var waitForProcess = false;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (arg.Equals("--duration-ms", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= args.Length)
                    throw new InvalidOperationException("Missing value for --duration-ms.");

                var durationMs = LiveLabCli.ParseInt32(args[++index]);
                if (durationMs <= 0)
                    throw new InvalidOperationException("--duration-ms must be positive.");

                duration = TimeSpan.FromMilliseconds(durationMs);
                continue;
            }

            if (arg.Equals("--poll-ms", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= args.Length)
                    throw new InvalidOperationException("Missing value for --poll-ms.");

                var pollMs = LiveLabCli.ParseInt32(args[++index]);
                if (pollMs <= 0)
                    throw new InvalidOperationException("--poll-ms must be positive.");

                pollInterval = TimeSpan.FromMilliseconds(pollMs);
                continue;
            }

            if (arg.Equals("--summary-ms", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= args.Length)
                    throw new InvalidOperationException("Missing value for --summary-ms.");

                var summaryMs = LiveLabCli.ParseInt32(args[++index]);
                if (summaryMs <= 0)
                    throw new InvalidOperationException("--summary-ms must be positive.");

                summaryInterval = TimeSpan.FromMilliseconds(summaryMs);
                continue;
            }

            if (arg.Equals("--attach-timeout-ms", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= args.Length)
                    throw new InvalidOperationException("Missing value for --attach-timeout-ms.");

                var attachTimeoutMs = LiveLabCli.ParseInt32(args[++index]);
                if (attachTimeoutMs <= 0)
                    throw new InvalidOperationException("--attach-timeout-ms must be positive.");

                attachTimeout = TimeSpan.FromMilliseconds(attachTimeoutMs);
                continue;
            }

            if (arg.Equals("--log-file", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= args.Length)
                    throw new InvalidOperationException("Missing value for --log-file.");

                logFilePath = args[++index].Trim();
                if (logFilePath.Length == 0)
                    throw new InvalidOperationException("--log-file must not be empty.");

                continue;
            }

            if (arg.Equals("--console-events", StringComparison.OrdinalIgnoreCase))
            {
                consoleEvents = true;
                continue;
            }

            if (arg.Equals("--include-noise", StringComparison.OrdinalIgnoreCase))
            {
                includeNoise = true;
                continue;
            }

            if (arg.Equals("--wait-for-process", StringComparison.OrdinalIgnoreCase))
            {
                waitForProcess = true;
                continue;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal))
                throw new InvalidOperationException($"Unknown option '{arg}'.");

            selectors.Add(arg);
        }

        if (selectors.Count == 0)
            throw new InvalidOperationException("At least one watch selector is required.");

        return new WatchOptions(
            [.. selectors],
            attachTimeout,
            duration,
            pollInterval,
            summaryInterval,
            consoleEvents,
            includeNoise,
            waitForProcess,
            logFilePath ?? CreateDefaultLogFilePath(selectors)
        );
    }

    private static async Task<ProcessMemory> AttachForWatchAsync(WatchOptions options)
    {
        if (!options.WaitForProcess)
            return ProcessMemory.Attach(ArcanumRuntimeOffsets.ProcessName);

        using var attachCancellation = options.AttachTimeout is { } timeout
            ? new CancellationTokenSource(timeout)
            : new CancellationTokenSource();
        ConsoleCancelEventHandler? handler = null;
        handler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            attachCancellation.Cancel();
        };

        Console.CancelKeyPress += handler;
        try
        {
            var timeoutText = options.AttachTimeout is { } value
                ? $" for up to {(int)value.TotalSeconds} second(s)"
                : string.Empty;
            Console.WriteLine(
                $"waiting for {ArcanumRuntimeOffsets.ProcessName} to start{timeoutText}. Press Ctrl+C to cancel."
            );

            while (true)
            {
                attachCancellation.Token.ThrowIfCancellationRequested();
                if (ProcessMemory.TryAttach(out var memory, ArcanumRuntimeOffsets.ProcessName))
                {
                    Console.WriteLine(
                        $"attached to {ArcanumRuntimeOffsets.ProcessName} pid {memory.ProcessId} at {ProcessMemory.FormatAddress(memory.ModuleBase)}."
                    );
                    return memory;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), attachCancellation.Token);
            }
        }
        catch (OperationCanceledException)
        {
            if (options.AttachTimeout is not null)
            {
                throw new InvalidOperationException(
                    $"Timed out waiting for {ArcanumRuntimeOffsets.ProcessName} after {(int)options.AttachTimeout.Value.TotalMilliseconds} ms."
                );
            }

            throw new InvalidOperationException($"Cancelled while waiting for {ArcanumRuntimeOffsets.ProcessName}.");
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }

    private static string CreateDefaultLogFilePath(IReadOnlyList<string> selectors)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var label =
            selectors.Count == 0
                ? "watch"
                : string.Join("-", selectors.Select(static value => SanitizePathSegment(value)).Take(3));
        if (label.Length == 0)
            label = "watch";

        return Path.Combine("artifacts", "logs", "ArcNET.LiveLab", $"watch-{timestamp}-{label}.jsonl");
    }

    private static string SanitizePathSegment(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];
        var count = 0;
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer[count++] = char.ToLowerInvariant(ch);
                continue;
            }

            if (count > 0 && buffer[count - 1] != '-')
                buffer[count++] = '-';
        }

        while (count > 0 && buffer[count - 1] == '-')
            count--;

        return count == 0 ? string.Empty : new string(buffer[..count]);
    }

    private sealed record WatchOptions(
        string[] Selectors,
        TimeSpan? AttachTimeout,
        TimeSpan? Duration,
        TimeSpan PollInterval,
        TimeSpan SummaryInterval,
        bool ConsoleEvents,
        bool IncludeNoise,
        bool WaitForProcess,
        string LogFilePath
    );
}
