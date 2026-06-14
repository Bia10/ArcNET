using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Tests;

public sealed class MobileEntityServiceTests
{
    [Test]
    public async Task ListMobiles_WhenMobilesExist_ProjectsDebuggerEntries()
    {
        var backend = new FakeMobileEntityBackend
        {
            Mobiles =
            [
                CreateIdentity(0x0000000201234562UL, "Pc", "hero", "proto#1000"),
                CreateIdentity(0x00000002089ABCDEUL, "Npc", "guard", "proto#2000"),
            ],
        };
        var service = new MobileEntityService(backend);

        var snapshot = service.ListMobiles(new MobileRosterRequest(CreateSession(), 64));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.ListMaxEntries).IsEqualTo(64);
        await Assert.That(snapshot.Mobiles).Count().IsEqualTo(2);
        await Assert.That(snapshot.Mobiles[0].DisplayText.Contains("proto#", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task SetStat_WhenRequestIsValid_ResolvesTargetAndFormatsSnapshot()
    {
        var mobileHandle = 0x0000000201234562UL;
        var backend = new FakeMobileEntityBackend
        {
            MutationResult = new MobileMutationExecutionResult(
                "main-thread-hook",
                "tig_window_display @ tig_window_display (Arcanum.exe+0x0011CF40)",
                "stat_base_set @ Arcanum.exe+0x000B0980",
                $"Set Strength on {RuntimeSemanticCatalog.FormatHandle(mobileHandle)} to 14",
                mobileHandle
            ),
        };
        backend.Identities[mobileHandle] = CreateIdentity(mobileHandle, "Npc", "guard", "proto#2000");
        var service = new MobileEntityService(backend);

        var snapshot = service.SetStat(
            new MobileStatWriteRequest(CreateSession(), Handle(mobileHandle), "strength", "14", "1500")
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.SetStatHandle).IsEqualTo(mobileHandle);
        await Assert.That(backend.SetStatId).IsEqualTo(0);
        await Assert.That(backend.SetStatValue).IsEqualTo(14);
        await Assert.That(snapshot.TargetText).Contains("Npc guard");
        await Assert.That(snapshot.StatNameText).IsEqualTo("Strength");
    }

    [Test]
    public async Task Kill_WhenRequestIsValid_FormatsKillSummary()
    {
        var mobileHandle = 0x0000000201234562UL;
        var backend = new FakeMobileEntityBackend
        {
            MutationResult = new MobileMutationExecutionResult(
                "main-thread-hook",
                "tig_window_display @ tig_window_display (Arcanum.exe+0x0011CF40)",
                "critter_kill @ Arcanum.exe+0x0005D900",
                $"Triggered critter_kill for {RuntimeSemanticCatalog.FormatHandle(mobileHandle)}",
                mobileHandle
            ),
        };
        backend.Identities[mobileHandle] = CreateIdentity(mobileHandle, "Npc", "guard", "proto#2000");
        var service = new MobileEntityService(backend);

        var snapshot = service.Kill(new MobileActionRequest(CreateSession(), Handle(mobileHandle), "1000"));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.KillHandle).IsEqualTo(mobileHandle);
        await Assert.That(snapshot.Status).IsEqualTo("Mobile kill triggered");
        await Assert.That(snapshot.Summary).Contains("native death path");
    }

    [Test]
    public async Task Spawn_WhenRequestIsValid_UsesAnchorAndReturnsCreatedHandle()
    {
        var anchorHandle = 0x0000000201234562UL;
        var spawnedHandle = 0x00000002089ABCDEUL;
        var prototypeHandle = 0x0000000200002002UL;
        var backend = new FakeMobileEntityBackend
        {
            MutationResult = new MobileMutationExecutionResult(
                "main-thread-hook",
                "tig_window_display @ tig_window_display (Arcanum.exe+0x0011CF40)",
                "obj_field_int64_get @ Arcanum.exe+0x00006DA0 · object_create @ Arcanum.exe+0x0003CBA0",
                $"Created {RuntimeSemanticCatalog.FormatHandle(spawnedHandle)}",
                spawnedHandle
            ),
        };
        backend.Identities[anchorHandle] = CreateIdentity(anchorHandle, "Pc", "hero", "proto#1000");
        var service = new MobileEntityService(backend);

        var snapshot = service.Spawn(new MobileSpawnRequest(CreateSession(), "player", prototypeHandle, "1000"));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.SpawnAnchorHandle).IsEqualTo(anchorHandle);
        await Assert.That(backend.SpawnPrototypeHandle).IsEqualTo(prototypeHandle);
        await Assert.That(snapshot.TargetHandleText).IsEqualTo(RuntimeSemanticCatalog.FormatHandle(spawnedHandle));
        await Assert.That(snapshot.PrototypeHandleText).IsEqualTo(RuntimeSemanticCatalog.FormatHandle(prototypeHandle));
    }

    private static string Handle(ulong handle) => RuntimeSemanticCatalog.FormatHandle(handle);

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

    private static LiveObjectIdentity CreateIdentity(
        ulong handle,
        string objectType,
        string objectLabel,
        string protoLabel
    ) =>
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
                ObjectTypeRaw: 0,
                ObjectTypeName: objectType,
                new LiveOid(2, null, objectLabel, objectLabel, objectLabel),
                new LiveOid(1, 1000, protoLabel, protoLabel, protoLabel),
                "0x0000000000004321"
            )
        );

    private sealed class FakeMobileEntityBackend : IMobileEntityBackend
    {
        public LivePlayerLocatorResult PlayerResolution { get; init; } =
            new(
                AutoResolvedHandle: 0x0000000201234562UL,
                "SingleLivePcInstance",
                "Your likely live player is Live PC instance hero.",
                [],
                [],
                []
            );

        public Dictionary<ulong, LiveObjectIdentity> Identities { get; } = [];

        public IReadOnlyList<LiveObjectIdentity> Mobiles { get; init; } = [];

        public MobileMutationExecutionResult MutationResult { get; init; } =
            new("dispatcher", "dispatcher-site", "detail", "result", 0);

        public int ListMaxEntries { get; private set; }
        public ulong SetStatHandle { get; private set; }
        public int SetStatId { get; private set; }
        public int SetStatValue { get; private set; }
        public ulong KillHandle { get; private set; }
        public ulong DespawnHandle { get; private set; }
        public ulong SpawnPrototypeHandle { get; private set; }
        public ulong SpawnAnchorHandle { get; private set; }

        public LivePlayerLocatorResult LocatePlayers(int processId) => PlayerResolution;

        public LiveObjectIdentity InspectHandle(int processId, ulong handle) =>
            Identities.TryGetValue(handle, out var identity) ? identity : default;

        public IReadOnlyList<LiveObjectIdentity> ListLiveMobiles(int processId, int maxEntries)
        {
            ListMaxEntries = maxEntries;
            return Mobiles;
        }

        public MobileMutationExecutionResult SetMobileStat(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int statId,
            int value,
            TimeSpan timeout
        )
        {
            SetStatHandle = handle;
            SetStatId = statId;
            SetStatValue = value;
            return MutationResult;
        }

        public MobileMutationExecutionResult KillMobile(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            TimeSpan timeout
        )
        {
            KillHandle = handle;
            return MutationResult;
        }

        public MobileMutationExecutionResult DespawnMobile(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            TimeSpan timeout
        )
        {
            DespawnHandle = handle;
            return MutationResult;
        }

        public MobileMutationExecutionResult SpawnMobile(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong prototypeHandle,
            ulong anchorHandle,
            TimeSpan timeout
        )
        {
            SpawnPrototypeHandle = prototypeHandle;
            SpawnAnchorHandle = anchorHandle;
            return MutationResult;
        }
    }
}
