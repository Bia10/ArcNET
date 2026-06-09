using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Windows;

public sealed class WatchService(IWatchBackend backend)
{
    [SupportedOSPlatform("windows")]
    public static WatchService Default { get; } = CreateDefault();

    public WatchHandle Start(WatchStartRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureWatchSupport(request.Session);

        var hooks = RuntimeWatchCatalog.ResolveSelectors(request.Preset.Selectors);
        if (hooks.Count == 0)
            throw new InvalidOperationException("The selected timeline preset does not resolve to any watch hooks.");

        var session = backend.StartWatch(request.Session.ProcessId, hooks);
        try
        {
            var snapshot = CreateSnapshot(
                request,
                totalEvents: 0,
                totalDroppedEvents: 0,
                totalContentionDrops: 0,
                totalWarnings: 0,
                []
            );
            return new WatchHandle(session, request, snapshot);
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    public WatchSnapshot Poll(WatchHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);

        var poll = handle.Session.ReadSince(handle.LastSequence);
        handle.LastSequence = poll.WriteSequence;
        handle.TotalDroppedEvents += poll.DroppedEvents;
        handle.TotalContentionDrops += poll.ContentionDrops;
        handle.TotalWarnings += poll.InconsistentRecords;

        var newEvents = poll.Events.Select(CreateEventSnapshot).ToArray();
        handle.TotalEvents += newEvents.Length;
        handle.AddEvents(newEvents, handle.Request.EventCapacity);

        var snapshot = CreateSnapshot(
            handle.Request,
            handle.TotalEvents,
            handle.TotalDroppedEvents,
            handle.TotalContentionDrops,
            handle.TotalWarnings,
            [.. handle.Events]
        );
        handle.UpdateSnapshot(snapshot);
        return snapshot;
    }

    private static void EnsureWatchSupport(AttachedSessionSnapshot session)
    {
        if (!session.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.WatchHooks))
        {
            throw new InvalidOperationException(
                "This runtime does not currently support live watch hooks. Use the capability banner to stay in read-mostly mode."
            );
        }
    }

    private static WatchSnapshot CreateSnapshot(
        WatchStartRequest request,
        int totalEvents,
        int totalDroppedEvents,
        int totalContentionDrops,
        int totalWarnings,
        IReadOnlyList<WatchEventSnapshot> events
    )
    {
        var status =
            totalEvents == 0
                ? $"Watching {request.Preset.DisplayName} on {request.Session.DisplayName}. No events captured yet."
                : $"Watching {request.Preset.DisplayName} on {request.Session.DisplayName}.";
        var summary =
            $"Events {totalEvents} · dropped {totalDroppedEvents} · contention {totalContentionDrops} · warnings {totalWarnings}";
        return new WatchSnapshot(
            DateTimeOffset.UtcNow,
            IsRunning: true,
            status,
            summary,
            request.Preset.DisplayName,
            totalEvents,
            totalDroppedEvents,
            totalContentionDrops,
            totalWarnings,
            events
        );
    }

    private static WatchEventSnapshot CreateEventSnapshot(RuntimeWatchCapturedEvent capturedEvent)
    {
        var projection = WatchEventProjector.Project(capturedEvent);
        var stackPreview = string.Join(
            ", ",
            Enumerable.Range(0, 4).Select(index => $"d{index}=0x{capturedEvent.StackDwords.Get(index):X8}")
        );

        return new WatchEventSnapshot(
            capturedEvent.Sequence,
            capturedEvent.Definition.Key,
            capturedEvent.Definition.EventName,
            projection.SemanticEvent,
            capturedEvent.Definition.Area,
            capturedEvent.Definition.Site,
            ProcessMemory.FormatAddress((nint)(long)capturedEvent.ReturnAddress),
            $"0x{capturedEvent.CallerRva:X8}",
            projection.Signature,
            projection.Summary,
            projection.SuggestedHandleHex,
            projection.CandidateHandles,
            stackPreview
        );
    }

    [SupportedOSPlatform("windows")]
    private static WatchService CreateDefault() => new(new WatchBackend());
}
