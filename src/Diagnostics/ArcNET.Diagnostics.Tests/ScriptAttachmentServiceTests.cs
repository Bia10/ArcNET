using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Tests;

public sealed class ScriptAttachmentServiceTests
{
    [Test]
    public async Task Read_WhenSessionCannotInvokeFunctions_ReturnsUnavailableSnapshot()
    {
        var service = new ScriptAttachmentService(new FakeScriptAttachmentBackend());

        var snapshot = service.Read(
            new ScriptAttachmentRequest(
                CreateSession() with
                {
                    Capabilities = new RuntimeCapabilityReport(
                        RuntimeSupportLevel.Exploratory,
                        DiagnosticsCapability.ReadMemory,
                        []
                    ),
                },
                "player",
                "dialog"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Script attachment unavailable");
        await Assert.That(snapshot.Script).IsNull();
    }

    [Test]
    public async Task Read_ParsesNamedAttachmentPointAndProjectsTypedScriptData()
    {
        var backend = CreateBackendWithPlayer();
        var service = new ScriptAttachmentService(backend);

        var snapshot = service.Read(new ScriptAttachmentRequest(CreateSession(), "player", "dialog"));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.AttachmentPoint).IsEqualTo(9);
        await Assert.That(snapshot.AttachmentPointName).IsEqualTo(RuntimeSemanticCatalog.AttachmentPointName(9));
        await Assert.That(snapshot.Script).IsNotNull();
        await Assert.That(snapshot.Script!.Counters).Count().IsEqualTo(4);
        await Assert.That(snapshot.Script.Counters[2]).IsEqualTo(7);
        await Assert.That(snapshot.Script.IsEmpty).IsFalse();
        await Assert.That(snapshot.TargetHandleText).IsEqualTo("0x0000000201234567");
        await Assert.That(snapshot.TargetText).Contains("Pc mob:guid-a");
        await Assert.That(snapshot.Notes).Contains("Resolved one player.");
        await Assert.That(backend.ReadCalls).HasSingleItem();
        await Assert.That(backend.ReadCalls[0].Handle).IsEqualTo(0x0000000201234567ul);
        await Assert.That(backend.ReadCalls[0].AttachmentPoint).IsEqualTo(9);
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

    private static FakeScriptAttachmentBackend CreateBackendWithPlayer()
    {
        var backend = new FakeScriptAttachmentBackend
        {
            PlayerResolution = new LivePlayerLocatorResult(
                0x0000000201234567,
                "SingleLivePcInstance",
                "Resolved one player.",
                [],
                [],
                []
            ),
            Payload = new ScriptAttachmentPayload(
                new ScriptAttachmentRecordSnapshot(
                    1234,
                    0x01020304,
                    "0x01020304",
                    0x08070605,
                    "0x08070605",
                    [5, 6, 7, 8],
                    IsEmpty: false
                ),
                new NativeReadSnapshot(
                    "obj_array_field_script_get",
                    "Arcanum.exe+obj_array_field_script_get",
                    "Fake script attachment read.",
                    "main-thread-hook",
                    "dispatcher-site",
                    "Completed",
                    0,
                    "0x00000000 (0)",
                    "0x00000000 (0)"
                )
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

    private sealed class FakeScriptAttachmentBackend : IScriptAttachmentBackend
    {
        private readonly Dictionary<ulong, LiveObjectIdentity> _inspections = [];

        public LivePlayerLocatorResult PlayerResolution { get; init; }
        public ScriptAttachmentPayload Payload { get; init; } =
            new(
                new ScriptAttachmentRecordSnapshot(0, 0, "0x00000000", 0, "0x00000000", [0, 0, 0, 0], true),
                new NativeReadSnapshot("", "", "", "", "", "", 0, "", "")
            );
        public List<ReadCall> ReadCalls { get; } = [];

        public void SetInspection(ulong handle, LiveObjectIdentity identity) => _inspections[handle] = identity;

        public LivePlayerLocatorResult LocatePlayers(int processId) => PlayerResolution;

        public LiveObjectIdentity InspectHandle(int processId, ulong handle) => _inspections[handle];

        public ScriptAttachmentPayload ReadAttachment(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int attachmentPoint
        )
        {
            ReadCalls.Add(new ReadCall(handle, attachmentPoint));
            return Payload;
        }
    }

    private readonly record struct ReadCall(ulong Handle, int AttachmentPoint);
}
