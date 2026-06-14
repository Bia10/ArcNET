using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Tests;

public sealed class WatchServiceTests
{
    [Test]
    public async Task StartAndPoll_WhenWatchSessionEmitsEvents_ProjectsRecentTimelineFeed()
    {
        var backend = new FakeWatchBackend();
        backend.Results.Enqueue(
            new RuntimeWatchReadResult(
                2,
                DroppedEvents: 1,
                InconsistentRecords: 0,
                ContentionDrops: 2,
                [
                    CreateCapturedEvent(RuntimeWatchHookId.LevelRecalc, 1),
                    CreateCapturedEvent(RuntimeWatchHookId.ItemInsert, 2),
                ]
            )
        );
        backend.Results.Enqueue(
            new RuntimeWatchReadResult(
                3,
                DroppedEvents: 0,
                InconsistentRecords: 1,
                ContentionDrops: 0,
                [CreateCapturedEvent(RuntimeWatchHookId.ObjectCreate, 3)]
            )
        );
        var service = new WatchService(backend);
        using var handle = service.Start(
            new WatchStartRequest(CreateValidatedSession(), CreateSessionCorePreset(), EventCapacity: 2)
        );

        var first = service.Poll(handle);
        var second = service.Poll(handle);

        await Assert.That(first.TotalEvents).IsEqualTo(2);
        await Assert.That(first.TotalDroppedEvents).IsEqualTo(1);
        await Assert.That(first.TotalContentionDrops).IsEqualTo(2);
        await Assert.That(first.Events.Count).IsEqualTo(2);
        await Assert.That(second.TotalEvents).IsEqualTo(3);
        await Assert.That(second.TotalWarnings).IsEqualTo(1);
        await Assert.That(second.Events.Count).IsEqualTo(2);
        await Assert.That(second.Events[0].HookKey).IsEqualTo("item-insert");
        await Assert.That(second.Events[1].HookKey).IsEqualTo("object-create");
        await Assert.That(second.Events[0].SemanticEvent).IsEqualTo("ItemInserted");
        await Assert
            .That(second.Events[0].SuggestedHandleHex)
            .IsEqualTo(RuntimeSemanticCatalog.FormatHandle(ComposeHandle(2, 9)));
        await Assert.That(second.Events[0].CandidateHandles.Count).IsEqualTo(2);
        await Assert.That(second.Events[1].Signature.Contains("ObjectCreate", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Start_WhenSessionDoesNotSupportWatchHooks_Throws()
    {
        var backend = new FakeWatchBackend();
        var service = new WatchService(backend);

        await Assert
            .That(() => service.Start(new WatchStartRequest(CreateUnsupportedSession(), CreateSessionCorePreset())))
            .Throws<InvalidOperationException>();
    }

    private static AttachedSessionSnapshot CreateValidatedSession() =>
        new(
            DateTimeOffset.UtcNow,
            SessionOrigin.Attach,
            "Arcanum.exe (PID 4242)",
            "Attached live session",
            @"C:\Games\Arcanum\Arcanum.exe @ 0x00400000",
            "Arcanum",
            4242,
            HasExited: false,
            new RuntimeFingerprint(
                "Arcanum",
                4242,
                RuntimeKind.Classic,
                "Arcanum.exe",
                @"C:\Games\Arcanum\Arcanum.exe",
                "0x00400000",
                3538944,
                3538944,
                DateTime.UtcNow
            ),
            new RuntimeProfileSnapshot(
                "validated-classic",
                "Arcanum.exe validated runtime profile",
                RuntimeKind.Classic,
                RuntimeSupportLevel.Validated,
                SupportsCatalogRvas: true,
                "Validated classic profile.",
                ModuleSha256: null,
                HashError: null
            ),
            new RuntimeCapabilityReport(
                RuntimeSupportLevel.Validated,
                DiagnosticsCapability.ReadMemory
                    | DiagnosticsCapability.ResolveRuntimeProfile
                    | DiagnosticsCapability.WatchHooks,
                []
            ),
            LaunchPreview: null,
            Notes: []
        );

    private static AttachedSessionSnapshot CreateUnsupportedSession() =>
        CreateValidatedSession() with
        {
            Capabilities = new RuntimeCapabilityReport(
                RuntimeSupportLevel.Exploratory,
                DiagnosticsCapability.ReadMemory | DiagnosticsCapability.ResolveRuntimeProfile,
                ["Watch hooks unavailable."]
            ),
        };

    private static TimelinePresetDescriptor CreateSessionCorePreset() =>
        new(
            "session-core",
            "Session Core",
            "Human-first session coverage.",
            ["session-core"],
            ["level-recalc", "item-insert"],
            ["Progression", "Inventory"],
            UsesHighVolumeHooks: false
        );

    private static RuntimeWatchCapturedEvent CreateCapturedEvent(RuntimeWatchHookId hookId, uint sequence)
    {
        var stack = new RuntimeWatchStackCapture();
        var primaryHandle = ComposeHandle((int)sequence, sequence + 7);
        stack.Set(0, unchecked((uint)primaryHandle));
        stack.Set(1, unchecked((uint)(primaryHandle >> 32)));

        var secondaryHandle = ComposeHandle((int)sequence + 10, sequence + 11);
        stack.Set(2, unchecked((uint)secondaryHandle));
        stack.Set(3, unchecked((uint)(secondaryHandle >> 32)));
        stack.Set(4, sequence + 4);
        stack.Set(5, sequence + 5);
        stack.Set(6, sequence + 6);
        stack.Set(7, sequence + 7);
        return new RuntimeWatchCapturedEvent(
            RuntimeWatchCatalog.GetDefinition(hookId),
            sequence,
            0x00401000 + sequence,
            0x00001000 + sequence,
            stack
        );
    }

    private static ulong ComposeHandle(int index, uint sequence) =>
        ((ulong)(uint)index << RuntimeOffsets.ObjHandleIndexShift)
        | ((ulong)sequence << RuntimeOffsets.ObjHandleSequenceShift)
        | RuntimeOffsets.ObjHandleMarkerValue;

    private sealed class FakeWatchBackend : IWatchBackend
    {
        public Queue<RuntimeWatchReadResult> Results { get; } = [];

        public IWatchSession StartWatch(int processId, IReadOnlyList<RuntimeWatchHookDefinition> hooks) =>
            new FakeWatchSession(Results);
    }

    private sealed class FakeWatchSession(Queue<RuntimeWatchReadResult> results) : IWatchSession
    {
        public RuntimeWatchReadResult ReadSince(uint lastSequence) =>
            results.Count > 0 ? results.Dequeue() : new RuntimeWatchReadResult(lastSequence, 0, 0, 0, []);

        public void Dispose() { }
    }
}
