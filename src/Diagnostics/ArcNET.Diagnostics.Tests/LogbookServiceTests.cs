using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Tests;

public sealed class LogbookServiceTests
{
    [Test]
    public async Task Read_WhenSessionCannotInvokeFunctions_ReturnsUnavailableSnapshot()
    {
        var service = new LogbookService(new FakeLogbookBackend());

        var snapshot = await service.ReadAsync(
            new LogbookRequest(
                CreateSession() with
                {
                    Capabilities = new RuntimeCapabilityReport(
                        RuntimeSupportLevel.Exploratory,
                        DiagnosticsCapability.ReadMemory,
                        []
                    ),
                },
                "player",
                "all"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Logbook unavailable");
        await Assert.That(snapshot.Data.RumorsAndNotes).IsNull();
    }

    [Test]
    public async Task Read_All_ReturnsTypedPagesAndMergedNotes()
    {
        var backend = CreateBackendWithPlayer();
        var service = new LogbookService(backend);

        var snapshot = await service.ReadAsync(new LogbookRequest(CreateSession(), "player", "all"));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Page).IsEqualTo(LogbookPage.All);
        await Assert.That(snapshot.TargetHandleText).IsEqualTo("0x0000000201234567");
        await Assert.That(snapshot.Data.RumorsAndNotes).IsNotNull();
        await Assert.That(snapshot.Data.Quests).IsNotNull();
        await Assert.That(snapshot.Data.KeyringContents).IsNotNull();
        await Assert.That(snapshot.Data.RumorsAndNotes!.Entries.Single().RumorId).IsEqualTo(77);
        await Assert.That(snapshot.Data.Quests!.Entries.Single().Label).IsEqualTo("Find the bridge");
        await Assert.That(snapshot.Data.KeyringContents!.Entries.Single().Name).IsEqualTo("Tarant Sewer Key");
        await Assert.That(snapshot.Notes).Contains("Resolved one player.");
        await Assert.That(snapshot.Notes).Contains("Catalog loaded from game install.");
        await Assert.That(backend.ReadCalls).HasSingleItem();
        await Assert.That(backend.ReadCalls[0].Page).IsEqualTo(LogbookPage.All);
    }

    [Test]
    public async Task Read_UsesPageAliases()
    {
        var backend = CreateBackendWithPlayer();
        var service = new LogbookService(backend);

        var snapshot = await service.ReadAsync(new LogbookRequest(CreateSession(), "player", "keys"));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Page).IsEqualTo(LogbookPage.KeyringContents);
        await Assert.That(backend.ReadCalls.Last().Page).IsEqualTo(LogbookPage.KeyringContents);
    }

    [Test]
    public async Task Read_WhenWorkspacePathOverrideIsProvided_PassesOverrideToBackend()
    {
        var backend = CreateBackendWithPlayer();
        var service = new LogbookService(backend);
        var workspacePath = @"C:\Games\Arcanum\modules\test-module";

        var snapshot = await service.ReadAsync(
            new LogbookRequest(
                CreateSession() with
                {
                    Fingerprint = new RuntimeFingerprint(
                        "Arcanum",
                        4242,
                        RuntimeKind.Classic,
                        "Arcanum.exe",
                        "",
                        "0x00400000",
                        3_538_944,
                        2_048_000,
                        DateTime.UtcNow
                    ),
                },
                "player",
                "all",
                workspacePath
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.ReadCalls).HasSingleItem();
        await Assert.That(backend.ReadCalls[0].WorkspacePath).IsEqualTo(workspacePath);
    }

    private static AttachedSessionSnapshot CreateSession() =>
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
                3_538_944,
                2_048_000,
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
                    | DiagnosticsCapability.ReadStructuredState
                    | DiagnosticsCapability.InvokeFunctions,
                []
            ),
            LaunchPreview: null,
            Notes: []
        );

    private static FakeLogbookBackend CreateBackendWithPlayer()
    {
        var backend = new FakeLogbookBackend
        {
            PlayerResolution = new LivePlayerLocatorResult(
                0x0000000201234567,
                "SingleLivePcInstance",
                "Resolved one player.",
                [],
                [],
                []
            ),
            Result = new LogbookReadResult(
                new LogbookPayload(
                    new RumorLogbookPageSnapshot(
                        8,
                        UsesDumbText: false,
                        [
                            new RumorLogbookEntrySnapshot(
                                77,
                                new(10, 200),
                                false,
                                "A quiet rumor",
                                "A quiet rumor",
                                null
                            ),
                        ],
                        CreateNativeRead("rumor_get_logbook_data", 1)
                    ),
                    new QuestLogbookPageSnapshot(
                        8,
                        UsesDumbText: false,
                        [
                            new QuestLogbookEntrySnapshot(
                                42,
                                new(11, 300),
                                2,
                                "Accepted",
                                "Find the bridge",
                                "Find the bridge",
                                "Find the bridge",
                                null
                            ),
                        ],
                        CreateNativeRead("quest_get_logbook_data", 1)
                    ),
                    new ReputationLogbookPageSnapshot(
                        [new ReputationLogbookEntrySnapshot(12, new(12, 400), "Sold Your Soul")],
                        CreateNativeRead("reputation_get_logbook_data", 1)
                    ),
                    new BlessingCurseLogbookPageSnapshot(
                        [new BlessingCurseLogbookEntrySnapshot("Blessing", 5, new(13, 500), "The Gift")],
                        [CreateNativeRead("bless_get_logbook_data", 1), CreateNativeRead("curse_get_logbook_data", 0)]
                    ),
                    new KillsAndInjuriesLogbookPageSnapshot(
                        [new KillLogbookSummaryEntrySnapshot("total_kills", "Total Kills", 0, null, 12)],
                        [
                            new InjuryLogbookEntrySnapshot(
                                64,
                                1001,
                                "Bandit",
                                2,
                                "Crippled leg",
                                true,
                                "Active",
                                "Crippled leg by Bandit"
                            ),
                        ],
                        CreateNativeRead("logbook_get_kills", 12)
                    ),
                    new BackgroundLogbookPageSnapshot(
                        4,
                        1004,
                        "Raised by Scholars",
                        "You grew up around books.",
                        "Raised by Scholars",
                        "You grew up around books.",
                        CreateNativeRead("background_get", 4),
                        CreateNativeRead("background_text_get", 1004)
                    ),
                    new KeyringLogbookPageSnapshot([new KeyringLogbookEntrySnapshot(0, 55, "Tarant Sewer Key")])
                ),
                ["Catalog loaded from game install."]
            ),
        };
        backend.SetInspection(0x0000000201234567, CreateIdentity(0x0000000201234567));
        return backend;
    }

    private static LiveObjectIdentity CreateIdentity(ulong handle) =>
        new(
            RuntimeSemanticCatalog.FormatHandle(handle),
            LooksLikeHandle: true,
            "PoolEntry",
            PoolIndex: 1,
            BucketIndex: 0,
            SlotIndex: 1,
            EntryAddress: "0x00001000",
            ObjectAddress: "0x00001004",
            Status: (byte)'H',
            Sequence: 1,
            ExpectedSequence: 1,
            new LiveObjectHeader(
                ObjectTypeRaw: 15,
                ObjectTypeName: "Pc",
                new LiveOid(2, null, "guid-a", "mob:guid-a", "mob:guid-a"),
                new LiveOid(1, 1000, "guid-b", "proto#1000", "proto#1000"),
                "0x0000000000004321"
            )
        );

    private static NativeReadSnapshot CreateNativeRead(string functionKey, int value) =>
        new(
            functionKey,
            $"Arcanum.exe+{functionKey}",
            $"Fake read for {functionKey}.",
            "main-thread-hook",
            "dispatcher-site",
            "Completed",
            value,
            $"0x{unchecked((uint)value):X8} ({value})",
            "0x00000000 (0)"
        );

    private sealed class FakeLogbookBackend : ILogbookBackend
    {
        private readonly Dictionary<ulong, LiveObjectIdentity> _inspections = [];

        public LivePlayerLocatorResult PlayerResolution { get; init; }
        public LogbookReadResult Result { get; init; } =
            new(new LogbookPayload(null, null, null, null, null, null, null), []);
        public List<ReadCall> ReadCalls { get; } = [];

        public void SetInspection(ulong handle, LiveObjectIdentity identity) => _inspections[handle] = identity;

        public LivePlayerLocatorResult LocatePlayers(int processId) => PlayerResolution;

        public LiveObjectIdentity InspectHandle(int processId, ulong handle) => _inspections[handle];

        public Task<LogbookReadResult> ReadLogbookAsync(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            LogbookPage page,
            string workspacePath
        )
        {
            ReadCalls.Add(new ReadCall(handle, page, workspacePath));
            return Task.FromResult(Result);
        }
    }

    private readonly record struct ReadCall(ulong Handle, LogbookPage Page, string WorkspacePath);
}
