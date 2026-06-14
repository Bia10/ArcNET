using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Tests;

public sealed class InventoryEditorServiceTests
{
    [Test]
    public async Task CreateItem_WhenRequestIsValid_ResolvesOwnerAndFormatsSnapshot()
    {
        var ownerHandle = 0x0000000201234562UL;
        var prototypeHandle = 0x0000000200001402UL;
        var itemHandle = 0x00000002089ABCDEUL;
        var backend = new FakeInventoryEditorBackend
        {
            PlayerResolution = new LivePlayerLocatorResult(
                AutoResolvedHandle: ownerHandle,
                "SingleLivePcInstance",
                "Your likely live player is Live PC instance hero.",
                [],
                [],
                []
            ),
            CreateResult = new InventoryCreateExecutionResult(
                "main-thread-hook",
                "tig_window_display @ tig_window_display (Arcanum.exe+0x0011CF40)",
                "object_create @ Arcanum.exe+0x0003CBA0 · item_insert @ Arcanum.exe+0x00066640",
                $"Created {RuntimeSemanticCatalog.FormatHandle(itemHandle)}",
                itemHandle
            ),
        };
        backend.Identities[ownerHandle] = CreateIdentity(ownerHandle, "Pc", "hero", "proto#1000");
        var service = new InventoryEditorService(backend);

        var snapshot = service.CreateItem(
            new InventoryCreateRequest(CreateSession(), "player", prototypeHandle, "weapon", "1500")
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.CreateOwnerHandle).IsEqualTo(ownerHandle);
        await Assert.That(backend.CreatePrototypeHandle).IsEqualTo(prototypeHandle);
        await Assert.That(backend.CreateInventoryLocation).IsEqualTo(1004);
        await Assert.That(snapshot.OwnerTargetText).Contains("Pc hero");
        await Assert.That(snapshot.ItemHandleText).IsEqualTo(RuntimeSemanticCatalog.FormatHandle(itemHandle));
        await Assert.That(snapshot.InventoryLocationText).IsEqualTo("Weapon (1004)");
        await Assert.That(snapshot.DispatcherText).Contains("main-thread-hook");
    }

    [Test]
    public async Task DestroyItem_WhenParentHandleExists_FormatsDetachedOwnerSummary()
    {
        var ownerHandle = 0x0000000201234562UL;
        var itemHandle = 0x00000002089ABCDEUL;
        var backend = new FakeInventoryEditorBackend
        {
            DestroyResult = new InventoryDestroyExecutionResult(
                "main-thread-hook",
                "tig_window_display @ tig_window_display (Arcanum.exe+0x0011CF40)",
                "obj_field_handle_get @ Arcanum.exe+0x00006E80 · item_force_remove @ Arcanum.exe+0x00067860 · object_destroy @ Arcanum.exe+0x0003CCA0",
                $"Removed {RuntimeSemanticCatalog.FormatHandle(itemHandle)} from {RuntimeSemanticCatalog.FormatHandle(ownerHandle)} and destroyed it.",
                ownerHandle
            ),
        };
        backend.Identities[itemHandle] = CreateIdentity(itemHandle, "Weapon", "item-guid", "proto#14001");
        backend.Identities[ownerHandle] = CreateIdentity(ownerHandle, "Pc", "hero", "proto#1000");
        var service = new InventoryEditorService(backend);

        var snapshot = service.DestroyItem(
            new InventoryDestroyRequest(CreateSession(), snapshotHandle(itemHandle), "1000")
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.DestroyItemHandle).IsEqualTo(itemHandle);
        await Assert.That(snapshot.OwnerTargetText).Contains("Pc hero");
        await Assert.That(snapshot.Summary).Contains("detaching");
        await Assert.That(snapshot.ResultText).Contains("destroyed");
    }

    private static string snapshotHandle(ulong handle) => RuntimeSemanticCatalog.FormatHandle(handle);

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

    private sealed class FakeInventoryEditorBackend : IInventoryEditorBackend
    {
        public LivePlayerLocatorResult PlayerResolution { get; init; }
        public Dictionary<ulong, LiveObjectIdentity> Identities { get; } = [];
        public InventoryCreateExecutionResult CreateResult { get; init; } =
            new("dispatcher", "dispatcher-site", "detail", "result", 0x00000002089ABCDEUL);
        public InventoryDestroyExecutionResult DestroyResult { get; init; } =
            new("dispatcher", "dispatcher-site", "detail", "result", 0);

        public ulong CreateOwnerHandle { get; private set; }
        public ulong CreatePrototypeHandle { get; private set; }
        public int CreateInventoryLocation { get; private set; }
        public ulong DestroyItemHandle { get; private set; }

        public LivePlayerLocatorResult LocatePlayers(int processId) => PlayerResolution;

        public LiveObjectIdentity InspectHandle(int processId, ulong handle) =>
            Identities.TryGetValue(handle, out var identity) ? identity : default;

        public InventoryCreateExecutionResult CreateItem(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong ownerHandle,
            ulong prototypeHandle,
            int inventoryLocation,
            TimeSpan timeout
        )
        {
            CreateOwnerHandle = ownerHandle;
            CreatePrototypeHandle = prototypeHandle;
            CreateInventoryLocation = inventoryLocation;
            return CreateResult;
        }

        public InventoryDestroyExecutionResult DestroyItem(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong itemHandle,
            TimeSpan timeout
        )
        {
            DestroyItemHandle = itemHandle;
            return DestroyResult;
        }
    }
}
