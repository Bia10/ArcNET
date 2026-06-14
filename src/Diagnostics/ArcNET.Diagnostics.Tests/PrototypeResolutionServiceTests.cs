using ArcNET.Core.Primitives;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.GameData.Workspace;

namespace ArcNET.Diagnostics.Tests;

public sealed class PrototypeResolutionServiceTests
{
    [Test]
    public async Task Resolve_WhenExplicitHandleIsProvided_SucceedsWithoutPrototypeLookup()
    {
        var handle = 0x0000000201234562UL;
        var backend = new FakePrototypeResolutionBackend
        {
            Inspection = CreateIdentity(handle, "Prototype Wolf", "Critter", 14001),
        };
        var service = new PrototypeResolutionService(backend);

        var snapshot = await service.ResolveAsync(
            new PrototypeResolutionRequest(
                CreateSession() with
                {
                    RuntimeProfile = CreateRuntimeProfile(supportsCatalogRvas: false),
                    Capabilities = new RuntimeCapabilityReport(
                        RuntimeSupportLevel.Exploratory,
                        DiagnosticsCapability.ReadMemory,
                        []
                    ),
                },
                "0x0000000201234562"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Status).IsEqualTo("Prototype handle resolved");
        await Assert.That(snapshot.Handle).IsEqualTo(handle);
        await Assert.That(snapshot.ResolutionSource).IsEqualTo("ExplicitHandle");
        await Assert.That(backend.ResolvePrototypeHandleCallCount).IsEqualTo(0);
    }

    [Test]
    public async Task Resolve_WhenPaletteEntryMatches_ResolvesProtoAndHandle()
    {
        var handle = 0x0000000201234562UL;
        var backend = new FakePrototypeResolutionBackend
        {
            PaletteEntries =
            [
                new PrototypePaletteEntry(
                    14001,
                    "Critter",
                    "proto/critters/wolf.pro",
                    "Wolf",
                    "A hungry wolf.",
                    "Critters",
                    "art/critters/wolf.art"
                ),
            ],
            Resolution = new PrototypeHandleResolutionResult(true, handle, "PrototypeLookupFunction"),
            Inspection = CreateIdentity(handle, "Prototype Wolf", "Critter", 14001),
        };
        var service = new PrototypeResolutionService(backend);

        var snapshot = await service.ResolveAsync(new PrototypeResolutionRequest(CreateSession(), "wolf"));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.ProtoNumber).IsEqualTo(14001);
        await Assert.That(snapshot.DisplayName).IsEqualTo("Wolf");
        await Assert.That(snapshot.AssetPath).IsEqualTo("proto/critters/wolf.pro");
        await Assert.That(snapshot.Handle).IsEqualTo(handle);
        await Assert.That(snapshot.ResolutionSource).IsEqualTo("PaletteExactMatch->PrototypeLookupFunction");
        await Assert.That(snapshot.ResolvedObject).IsNotNull();
        await Assert.That(snapshot.ResolvedObject!.ProtoNumber).IsEqualTo(14001);
        await Assert.That(backend.ResolvePrototypeHandleCallCount).IsEqualTo(1);
    }

    [Test]
    public async Task Resolve_WhenProtoNumberNeedsCatalogSupportAndSessionLacksIt_ReturnsUnavailableSnapshot()
    {
        var service = new PrototypeResolutionService(new FakePrototypeResolutionBackend());

        var snapshot = await service.ResolveAsync(
            new PrototypeResolutionRequest(
                CreateSession() with
                {
                    RuntimeProfile = CreateRuntimeProfile(supportsCatalogRvas: false),
                    Capabilities = new RuntimeCapabilityReport(
                        RuntimeSupportLevel.Exploratory,
                        DiagnosticsCapability.ReadMemory,
                        []
                    ),
                },
                "proto:14001"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Prototype resolution unavailable");
        await Assert.That(snapshot.Summary).Contains("catalog-backed runtime offsets");
    }

    [Test]
    public async Task Resolve_WhenWorkspacePathOverrideIsProvided_UsesOverrideForPaletteLookup()
    {
        var handle = 0x0000000201234562UL;
        var backend = new FakePrototypeResolutionBackend
        {
            PaletteEntries =
            [
                new PrototypePaletteEntry(
                    14001,
                    "Critter",
                    "proto/critters/wolf.pro",
                    "Wolf",
                    "A hungry wolf.",
                    "Critters",
                    "art/critters/wolf.art"
                ),
            ],
            Resolution = new PrototypeHandleResolutionResult(true, handle, "PrototypeLookupFunction"),
            Inspection = CreateIdentity(handle, "Prototype Wolf", "Critter", 14001),
        };
        var service = new PrototypeResolutionService(backend);
        var workspacePath = @"C:\Games\Arcanum\modules\test-module";

        var snapshot = await service.ResolveAsync(
            new PrototypeResolutionRequest(
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
                "wolf",
                workspacePath
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.LastPaletteWorkspacePath).IsEqualTo(workspacePath);
    }

    [Test]
    public async Task Resolve_WhenPlacedObjectGuidMatches_ResolvesPlacedPrototypeAndHandle()
    {
        var handle = 0x0000000201234562UL;
        var backend = new FakePrototypeResolutionBackend
        {
            StaticObjectEntries =
            [
                new StaticObjectCatalogEntry(
                    "Sector object",
                    "Tarant Barrel",
                    "Container",
                    "barrel-oid",
                    "2d5ee3db-f6d0-4c79-8dcb-1d4324a5e859",
                    13007,
                    "Barrel [13007]",
                    "maps/tarant/barrel.mob",
                    "Tile (441, 288)",
                    "Sector object - maps/tarant/barrel.mob"
                ),
            ],
            Resolution = new PrototypeHandleResolutionResult(true, handle, "PrototypeLookupFunction"),
            Inspection = CreateIdentity(handle, "Prototype Barrel", "Container", 13007),
        };
        var service = new PrototypeResolutionService(backend);

        var snapshot = await service.ResolveAsync(
            new PrototypeResolutionRequest(CreateSession(), "2d5ee3db-f6d0-4c79-8dcb-1d4324a5e859")
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.ProtoNumber).IsEqualTo(13007);
        await Assert.That(snapshot.DisplayName).IsEqualTo("Tarant Barrel");
        await Assert.That(snapshot.AssetPath).IsEqualTo("maps/tarant/barrel.mob");
        await Assert.That(snapshot.ResolutionSource).IsEqualTo("StaticObjectExactMatch->PrototypeLookupFunction");
        await Assert.That(snapshot.Handle).IsEqualTo(handle);
        await Assert.That(backend.ResolvePrototypeHandleCallCount).IsEqualTo(1);
    }

    [Test]
    public async Task Resolve_WhenPlacedObjectSearchIsAmbiguous_ReturnsUnavailableSnapshot()
    {
        var service = new PrototypeResolutionService(
            new FakePrototypeResolutionBackend
            {
                StaticObjectEntries =
                [
                    new StaticObjectCatalogEntry(
                        "Sector object",
                        "Bridge Door",
                        "Portal",
                        "door-1",
                        string.Empty,
                        1101,
                        "Bridge Door [1101]",
                        "maps/bridge/door1.mob",
                        "Tile (10, 10)",
                        "Sector object - maps/bridge/door1.mob"
                    ),
                    new StaticObjectCatalogEntry(
                        "Sector object",
                        "Bridge Door",
                        "Portal",
                        "door-2",
                        string.Empty,
                        1102,
                        "Bridge Door [1102]",
                        "maps/bridge/door2.mob",
                        "Tile (12, 10)",
                        "Sector object - maps/bridge/door2.mob"
                    ),
                ],
            }
        );

        var snapshot = await service.ResolveAsync(new PrototypeResolutionRequest(CreateSession(), "bridge door"));

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Ambiguous prototype reference");
        await Assert.That(snapshot.Summary).Contains("Placed-object lookup");
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
                3538944,
                3538944,
                DateTime.UtcNow
            ),
            CreateRuntimeProfile(supportsCatalogRvas: true),
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

    private static RuntimeProfileSnapshot CreateRuntimeProfile(bool supportsCatalogRvas) =>
        new(
            "validated-classic",
            "Arcanum.exe validated runtime profile",
            RuntimeKind.Classic,
            supportsCatalogRvas ? RuntimeSupportLevel.Validated : RuntimeSupportLevel.Exploratory,
            supportsCatalogRvas,
            supportsCatalogRvas ? "Validated classic profile." : "Exploratory profile.",
            ModuleSha256: null,
            HashError: null
        );

    private static LiveObjectIdentity CreateIdentity(
        ulong handle,
        string prototypeLabel,
        string objectType,
        int protoNumber
    ) =>
        new(
            RuntimeSemanticCatalog.FormatHandle(handle),
            LooksLikeHandle: true,
            "PoolEntry",
            PoolIndex: 5,
            BucketIndex: 0,
            SlotIndex: 5,
            EntryAddress: "0x00500000",
            ObjectAddress: "0x00500008",
            Status: (byte)'H',
            Sequence: 2,
            ExpectedSequence: 2,
            new LiveObjectHeader(
                1,
                objectType,
                new LiveOid(2, protoNumber, "guid", "Wolf", "Wolf"),
                new LiveOid(GameObjectGuid.OidTypeA, protoNumber, "proto-guid", prototypeLabel, prototypeLabel),
                RuntimeSemanticCatalog.FormatHandle(handle)
            )
        );

    private sealed class FakePrototypeResolutionBackend : IPrototypeResolutionBackend
    {
        public IReadOnlyList<PrototypePaletteEntry> PaletteEntries { get; init; } = [];
        public IReadOnlyList<StaticObjectCatalogEntry> StaticObjectEntries { get; init; } = [];
        public PrototypeHandleResolutionResult Resolution { get; init; } = new(false, 0, "PoolLookupMiss");
        public LiveObjectIdentity Inspection { get; init; }

        public int ResolvePrototypeHandleCallCount { get; private set; }
        public string? LastPaletteWorkspacePath { get; private set; }
        public string? LastStaticObjectWorkspacePath { get; private set; }

        public Task<IReadOnlyList<PrototypePaletteEntry>> LoadPaletteAsync(string workspacePath)
        {
            LastPaletteWorkspacePath = workspacePath;
            return Task.FromResult(PaletteEntries);
        }

        public Task<IReadOnlyList<StaticObjectCatalogEntry>> LoadStaticObjectCatalogAsync(string workspacePath)
        {
            LastStaticObjectWorkspacePath = workspacePath;
            return Task.FromResult(StaticObjectEntries);
        }

        public LiveObjectIdentity InspectHandle(int processId, ulong handle) => Inspection;

        public PrototypeHandleResolutionResult ResolvePrototypeHandle(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            int protoNumber
        )
        {
            ResolvePrototypeHandleCallCount++;
            return Resolution;
        }

        public LivePlayerLocatorResult LocatePlayers(int processId) =>
            new(null, "NotUsed", "Not used by prototype resolution.", [], [], []);
    }
}
