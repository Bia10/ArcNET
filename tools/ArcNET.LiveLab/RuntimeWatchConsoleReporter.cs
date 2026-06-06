using System.Runtime.Versioning;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal sealed class RuntimeWatchConsoleReporter
{
    private readonly bool _echoEvents;
    private readonly RuntimeWatchObjectResolver _resolver;
    private readonly TimeSpan _summaryInterval;
    private readonly Dictionary<string, int> _intervalEventCounts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _intervalSuppressedCounts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _intervalSignatures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _totalEventCounts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _totalSuppressedCounts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _totalSignatures = new(StringComparer.Ordinal);
    private DateTime _nextSummaryUtc;
    private int _intervalEvents;
    private int _intervalSuppressed;
    private int _totalEvents;
    private int _totalSuppressed;
    private int _totalDropped;
    private int _totalWarnings;

    public RuntimeWatchConsoleReporter(
        bool echoEvents,
        TimeSpan summaryInterval,
        RuntimeWatchObjectResolver resolver
    )
    {
        _echoEvents = echoEvents;
        _resolver = resolver;
        _summaryInterval = summaryInterval;
        _nextSummaryUtc = DateTime.UtcNow + summaryInterval;
    }

    public void WriteStart(
        string[] selectors,
        IReadOnlyList<RuntimeWatchHookDefinition> hooks,
        string logFilePath,
        bool includeNoise,
        ProcessMemory memory
    )
    {
        var selectorsText = string.Join(", ", selectors);
        var hooksText = string.Join(", ", hooks.Select(static hook => hook.Key));
        Console.WriteLine($"watch running for {selectorsText}");
        Console.WriteLine($"hooks: {hooksText}");
        Console.WriteLine($"process: {memory.ProcessId} at {ProcessMemory.FormatAddress(memory.ModuleBase)}");
        Console.WriteLine($"file log: {logFilePath}");
        Console.WriteLine($"name resolution: {_resolver.StatusText}");
        Console.WriteLine(
            includeNoise
                ? "console: summary mode; detailed event spam stays in the log file. Press Ctrl+C to stop."
                : "console: summary mode with Heartbeat/WillKos noise suppressed. Use --include-noise for the full stream. Press Ctrl+C to stop."
        );

        if (_echoEvents)
            Console.WriteLine("console events: enabled; each kept event will also print a human summary line.");
    }

    public void RecordEvent(RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent)
    {
        _totalEvents++;
        _intervalEvents++;

        var semanticEvent = RuntimeWatchEventInterpreter.SemanticEvent(capturedEvent);
        var signature = RuntimeWatchEventInterpreter.Signature(capturedEvent, _resolver);
        Increment(_totalEventCounts, semanticEvent);
        Increment(_intervalEventCounts, semanticEvent);
        Increment(_totalSignatures, signature);
        Increment(_intervalSignatures, signature);

        if (_echoEvents)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            Console.WriteLine(
                $"[{timestamp}] {semanticEvent} | {signature} | {RuntimeWatchEventInterpreter.Summary(capturedEvent, _resolver)}"
            );
        }
    }

    public void RecordSuppressed(RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent)
    {
        _totalSuppressed++;
        _intervalSuppressed++;

        var semanticEvent = RuntimeWatchEventInterpreter.SemanticEvent(capturedEvent);
        Increment(_totalSuppressedCounts, semanticEvent);
        Increment(_intervalSuppressedCounts, semanticEvent);
    }

    public void RecordOverflow(int droppedEvents, uint writeSequence)
    {
        _totalDropped += droppedEvents;
        Console.Error.WriteLine($"watch overflow: dropped {droppedEvents} events near sequence {writeSequence}.");
    }

    public void RecordWarning(int inconsistentRecords, uint writeSequence)
    {
        _totalWarnings += inconsistentRecords;
        Console.Error.WriteLine(
            $"watch warning: skipped {inconsistentRecords} inconsistent records near sequence {writeSequence}."
        );
    }

    public void WriteTargetExited() => Console.WriteLine("Arcanum exited; finalizing watch cleanup.");

    public void MaybeWriteSummary()
    {
        var now = DateTime.UtcNow;
        if (now < _nextSummaryUtc)
            return;

        _nextSummaryUtc = now + _summaryInterval;
        if (_intervalEvents == 0 && _intervalSuppressed == 0)
            return;

        var summary = FormatCounts(_intervalEventCounts);
        var hotSignatures = FormatSignatures(_intervalSignatures);
        var suppressed = FormatCounts(_intervalSuppressedCounts);
        var timestamp = DateTime.Now.ToString("HH:mm:ss");

        if (_intervalSuppressed > 0)
        {
            Console.WriteLine(
                $"[{timestamp}] +{_intervalEvents} kept | +{_intervalSuppressed} suppressed noise | {summary}{FormatHotSuffix(hotSignatures)} | suppressed: {suppressed}"
            );
        }
        else
        {
            Console.WriteLine($"[{timestamp}] +{_intervalEvents} kept | {summary}{FormatHotSuffix(hotSignatures)}");
        }

        _intervalEvents = 0;
        _intervalSuppressed = 0;
        _intervalEventCounts.Clear();
        _intervalSuppressedCounts.Clear();
        _intervalSignatures.Clear();
    }

    public void WriteEnd(string logFilePath)
    {
        MaybeWriteSummary();

        var totalSummary = FormatCounts(_totalEventCounts);
        Console.WriteLine(
            $"watch ended. kept {_totalEvents} events, suppressed {_totalSuppressed} noise, dropped {_totalDropped}, warnings {_totalWarnings}."
        );
        if (totalSummary.Length > 0)
            Console.WriteLine($"totals: {totalSummary}");

        var topSignatures = FormatSignatures(_totalSignatures);
        if (topSignatures.Length > 0)
            Console.WriteLine($"top repeated: {topSignatures}");

        if (_totalSuppressed > 0)
            Console.WriteLine($"suppressed totals: {FormatCounts(_totalSuppressedCounts)}");

        Console.WriteLine($"file log: {logFilePath}");
    }

    private static void Increment(Dictionary<string, int> counts, string key)
    {
        counts[key] = counts.GetValueOrDefault(key) + 1;
    }

    private static string FormatCounts(Dictionary<string, int> counts)
    {
        if (counts.Count == 0)
            return "no events";

        return string.Join(
            ", ",
            counts
                .OrderByDescending(static pair => pair.Value)
                .ThenBy(static pair => pair.Key, StringComparer.Ordinal)
                .Take(5)
                .Select(static pair => $"{pair.Key}={pair.Value}")
        );
    }

    private static string FormatSignatures(Dictionary<string, int> signatures)
    {
        var repeated = signatures
            .Where(static pair => pair.Value > 1)
            .OrderByDescending(static pair => pair.Value)
            .ThenBy(static pair => pair.Key, StringComparer.Ordinal)
            .Take(3)
            .Select(static pair => $"{pair.Key} x{pair.Value}")
            .ToArray();

        return repeated.Length == 0 ? string.Empty : string.Join("; ", repeated);
    }

    private static string FormatHotSuffix(string hotSignatures) =>
        hotSignatures.Length == 0 ? string.Empty : $" | hot: {hotSignatures}";
}
