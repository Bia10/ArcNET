using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Tests;

public sealed class ObjectProbeServiceTests
{
    [Test]
    public async Task Inspect_WhenSessionSupportsStructuredState_ProjectsDecodedObjectCards()
    {
        var backend = new FakeObjectProbeBackend
        {
            Results =
            [
                new LiveObjectInspection(
                    new LiveObjectIdentity(
                        "0xDEADBEEF12345678",
                        LooksLikeHandle: true,
                        "PoolEntry",
                        PoolIndex: 77,
                        BucketIndex: 1,
                        SlotIndex: 13,
                        EntryAddress: "0x00004044",
                        ObjectAddress: "0x00004048",
                        Status: (byte)'H',
                        Sequence: 9,
                        ExpectedSequence: 9,
                        new LiveObjectHeader(
                            ObjectTypeRaw: 15,
                            ObjectTypeName: "Pc",
                            new LiveOid(2, null, "guid-a", "mob:guid-a", "mob:guid-a"),
                            new LiveOid(1, 1234, "guid-b", "proto#1234", "proto#1234"),
                            "0x00000000ABCDEF01"
                        )
                    ),
                    [
                        new LiveObjectDetail("obj_f_hp_pts", "Hp Pts", "40", "obj_field_int32_get"),
                        new LiveObjectDetail("health_remaining", "Health Remaining", "33", "Computed"),
                        new LiveObjectDetail("stat_0", "Strength", "12", "stat_base_get"),
                        new LiveObjectDetail("stat_17", "Level", "5", "stat_base_get"),
                        new LiveObjectDetail("resistance_0", "Resistance / Normal", "10", "object_get_resistance"),
                        new LiveObjectDetail(
                            "basic_skill_0",
                            "Basic Skill / Bow",
                            "3 (Apprentice)",
                            "obj_array_field_int32_get"
                        ),
                        new LiveObjectDetail("obj_f_critter_gold", "Critter Gold", "250", "obj_field_int32_get"),
                    ]
                ),
            ],
        };
        var service = new ObjectProbeService(backend);

        var snapshot = service.Inspect(
            new ObjectProbeRequest(
                CreateStructuredSession(),
                ["0xDEADBEEF12345678", "0xDEADBEEF12345678", "invalid"],
                "selected watch event"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.RequestedHandles).IsEquivalentTo(["0xDEADBEEF12345678"]);
        await Assert.That(snapshot.Objects).HasSingleItem();
        await Assert.That(snapshot.Objects[0].ObjectTypeText).IsEqualTo("Pc");
        await Assert.That(snapshot.Objects[0].ObjectIdText).IsEqualTo("mob:guid-a");
        await Assert.That(snapshot.Objects[0].PrototypeText).IsEqualTo("proto#1234");
        await Assert.That(snapshot.Objects[0].Details).Count().IsEqualTo(7);
        await Assert.That(snapshot.Objects[0].Sections).Count().IsEqualTo(6);
        await Assert
            .That(snapshot.Objects[0].Sections.Select(static section => section.Title))
            .IsEquivalentTo(["Vitals", "Primary Stats", "Progression", "Resistances", "Basic Skills", "Resources"]);
        await Assert.That(snapshot.Objects[0].Sections[1].SourceText).IsEqualTo("Getter-backed stat read");
        await Assert.That(snapshot.Objects[0].Sections[4].SourceText).IsEqualTo("Getter-backed array read");
        await Assert.That(snapshot.Objects[0].Sections[0].Details[0].Value).IsEqualTo("40");
        await Assert.That(snapshot.Summary.Contains("expanded", StringComparison.Ordinal)).IsTrue();
        await Assert.That(snapshot.Objects[0].StatusText.Contains("PoolEntry", StringComparison.Ordinal)).IsTrue();
        await Assert.That(backend.InspectedHandles).IsEquivalentTo([0xDEADBEEF12345678ul]);
        await Assert.That(backend.IncludeExtendedDetails).IsTrue();
    }

    [Test]
    public async Task Inspect_WhenAggregateDetailsExist_KeepsStableSectionLabelsAndProjectsSourceText()
    {
        var backend = new FakeObjectProbeBackend
        {
            Results =
            [
                new LiveObjectInspection(
                    new LiveObjectIdentity(
                        "0x0000000000001234",
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
                    ),
                    [
                        new LiveObjectDetail("stat_0", "Strength", "8", "character_base_aggregate"),
                        new LiveObjectDetail("stat_1", "Dexterity", "8", "character_base_aggregate"),
                        new LiveObjectDetail("stat_11", "Speed", "9", "stat_base_get"),
                        new LiveObjectDetail(
                            "basic_skill_9",
                            "Basic Skill / Haggle",
                            "5 (Master)",
                            "character_base_aggregate"
                        ),
                    ]
                ),
            ],
        };
        var service = new ObjectProbeService(backend);

        var snapshot = service.Inspect(
            new ObjectProbeRequest(CreateStructuredSession(), ["0x0000000000001234"], "manual probe")
        );

        await Assert.That(snapshot.Objects).HasSingleItem();
        await Assert.That(snapshot.Objects[0].Sections).Count().IsEqualTo(3);
        await Assert.That(snapshot.Objects[0].Sections[0].Title).IsEqualTo("Primary Stats");
        await Assert.That(snapshot.Objects[0].Sections[0].SourceText).IsEqualTo("Experimental aggregate read");
        await Assert.That(snapshot.Objects[0].Sections[1].Title).IsEqualTo("Derived Stats");
        await Assert.That(snapshot.Objects[0].Sections[1].SourceText).IsEqualTo("Getter-backed stat read");
        await Assert.That(snapshot.Objects[0].Sections[2].Title).IsEqualTo("Basic Skills");
        await Assert.That(snapshot.Objects[0].Sections[2].SourceText).IsEqualTo("Experimental aggregate read");
        await Assert
            .That(snapshot.Summary.Contains("adjusted in-game character sheet", StringComparison.Ordinal))
            .IsFalse();
    }

    [Test]
    public async Task Inspect_WhenSessionCannotReadStructuredState_ReturnsUnavailableSnapshot()
    {
        var service = new ObjectProbeService(new FakeObjectProbeBackend());

        var snapshot = service.Inspect(
            new ObjectProbeRequest(
                CreateStructuredSession() with
                {
                    Capabilities = new RuntimeCapabilityReport(
                        RuntimeSupportLevel.Exploratory,
                        DiagnosticsCapability.ReadMemory,
                        ["Structured state unavailable."]
                    ),
                },
                ["0xDEADBEEF12345678"],
                "manual probe"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Objects).IsEmpty();
        await Assert.That(snapshot.Status).IsEqualTo("Object probe unavailable");
    }

    [Test]
    public async Task Inspect_WhenPlayerTokenProvided_UsesResolvedLivePlayerHandle()
    {
        var backend = new FakeObjectProbeBackend
        {
            PlayerResolution = new LivePlayerLocatorResult(
                AutoResolvedHandle: 0x1234,
                "SingleLivePcInstance",
                "Your likely live player is Live PC instance hero (0x0000000000001234).",
                [
                    new LivePlayerCandidate(
                        0x1234,
                        "0x0000000000001234",
                        "LiveInstance",
                        "Live PC instance hero (0x0000000000001234)",
                        "PoolEntry",
                        "Pc",
                        "hero",
                        "proto#1000",
                        1000,
                        "0x0000000000004321"
                    ),
                ],
                [],
                []
            ),
            Results =
            [
                new LiveObjectInspection(
                    new LiveObjectIdentity(
                        "0x0000000000001234",
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
                    ),
                    []
                ),
            ],
        };
        var service = new ObjectProbeService(backend);

        var snapshot = service.Inspect(
            new ObjectProbeRequest(CreateStructuredSession(), ["player"], "active player token")
        );

        await Assert.That(backend.LocatePlayersCallCount).IsEqualTo(1);
        await Assert.That(backend.InspectedHandles).IsEquivalentTo([0x1234ul]);
        await Assert.That(snapshot.RequestedHandles).IsEquivalentTo(["0x0000000000001234"]);
        await Assert.That(snapshot.Summary.Contains("likely live player", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Inspect_WhenInventoryHandleDetailsExist_GroupsThemIntoInventoryLinks()
    {
        var backend = new FakeObjectProbeBackend
        {
            Results =
            [
                new LiveObjectInspection(
                    new LiveObjectIdentity(
                        "0x0000000000002222",
                        LooksLikeHandle: true,
                        "PoolEntry",
                        PoolIndex: 2,
                        BucketIndex: 0,
                        SlotIndex: 2,
                        EntryAddress: "0x00002000",
                        ObjectAddress: "0x00002004",
                        Status: (byte)'H',
                        Sequence: 1,
                        ExpectedSequence: 1,
                        new LiveObjectHeader(
                            ObjectTypeRaw: 15,
                            ObjectTypeName: "Pc",
                            new LiveOid(2, null, "hero", "hero", "hero"),
                            new LiveOid(1, 1000, "proto#1000", "proto#1000", "proto#1000"),
                            "0x0000000000004321"
                        )
                    ),
                    [
                        new LiveObjectDetail(
                            "obj_f_critter_inventory_list_idx_0",
                            "Inventory Slot 0",
                            "0x0000000200000002",
                            "obj_array_field_handle_get"
                        ),
                        new LiveObjectDetail(
                            "obj_f_critter_inventory_list_idx_1",
                            "Inventory Slot 1",
                            "0x000000020000000A",
                            "obj_array_field_handle_get"
                        ),
                    ]
                ),
            ],
        };
        var service = new ObjectProbeService(backend);

        var snapshot = service.Inspect(
            new ObjectProbeRequest(CreateStructuredSession(), ["0x0000000000002222"], "manual probe")
        );

        await Assert.That(snapshot.Objects).HasSingleItem();
        await Assert.That(snapshot.Objects[0].Sections).HasSingleItem();
        await Assert.That(snapshot.Objects[0].Sections[0].Title).IsEqualTo("Inventory Links");
        await Assert.That(snapshot.Objects[0].Sections[0].SourceText).IsEqualTo("Getter-backed handle-array read");
        await Assert
            .That(snapshot.Objects[0].Sections[0].Details.Select(static detail => detail.Label))
            .IsEquivalentTo(["Inventory Slot 0", "Inventory Slot 1"]);
    }

    private static AttachedSessionSnapshot CreateStructuredSession() =>
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
                    | DiagnosticsCapability.ReadStructuredState
                    | DiagnosticsCapability.InvokeFunctions,
                []
            ),
            LaunchPreview: null,
            Notes: []
        );

    private sealed class FakeObjectProbeBackend : IObjectProbeBackend
    {
        public LivePlayerLocatorResult PlayerResolution { get; init; }
        public IReadOnlyList<LiveObjectInspection> Results { get; init; } = [];

        public List<ulong> InspectedHandles { get; } = [];
        public bool IncludeExtendedDetails { get; private set; }
        public int LocatePlayersCallCount { get; private set; }

        public LivePlayerLocatorResult LocatePlayers(int processId)
        {
            LocatePlayersCallCount++;
            return PlayerResolution;
        }

        public IReadOnlyList<LiveObjectInspection> InspectHandles(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            bool includeExtendedDetails,
            IReadOnlyList<ulong> handles
        )
        {
            InspectedHandles.AddRange(handles);
            IncludeExtendedDetails = includeExtendedDetails;
            return Results;
        }
    }
}
