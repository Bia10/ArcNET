using ArcNET.Core.Primitives;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Diagnostics.Windows;

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

        var snapshot = service.Resolve(
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

        var snapshot = service.Resolve(new PrototypeResolutionRequest(CreateSession(), "wolf"));

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

        var snapshot = service.Resolve(
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
        public PrototypeHandleResolutionResult Resolution { get; init; } = new(false, 0, "PoolLookupMiss");
        public LiveObjectIdentity Inspection { get; init; }

        public int ResolvePrototypeHandleCallCount { get; private set; }

        public IReadOnlyList<PrototypePaletteEntry> LoadPalette(string modulePath) => PaletteEntries;

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
