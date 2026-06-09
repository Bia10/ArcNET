using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Tests;

public sealed class ReadServiceTests
{
    [Test]
    public async Task Read_WhenSessionCannotInvokeFunctions_ReturnsUnavailableSnapshot()
    {
        var service = new ReadService(new FakeReadBackend());

        var snapshot = service.Read(
            new ReadRequest(
                CreateSession() with
                {
                    Capabilities = new RuntimeCapabilityReport(
                        RuntimeSupportLevel.Exploratory,
                        DiagnosticsCapability.ReadMemory,
                        []
                    ),
                },
                "story-state",
                []
            )
        );

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Read unavailable");
        await Assert.That(snapshot.NativeRead).IsNull();
    }

    [Test]
    public async Task Read_Quest_WithPlayerToken_ResolvesHandleAndProjectsQuestState()
    {
        var backend = new FakeReadBackend
        {
            PlayerResolution = new LivePlayerLocatorResult(
                0x0000000201234567,
                "SingleLivePcInstance",
                "Your likely live player is Live PC instance hero (0x0000000201234567).",
                [],
                [],
                []
            ),
        };
        backend.SetInspection(
            0x0000000201234567,
            new LiveObjectIdentity(
                "0x0000000201234567",
                LooksLikeHandle: true,
                "PoolEntry",
                PoolIndex: 11,
                BucketIndex: 0,
                SlotIndex: 11,
                EntryAddress: "0x00001000",
                ObjectAddress: "0x00001004",
                Status: (byte)'H',
                Sequence: 3,
                ExpectedSequence: 3,
                new LiveObjectHeader(
                    ObjectTypeRaw: 15,
                    ObjectTypeName: "Pc",
                    new LiveOid(2, null, "guid-a", "mob:guid-a", "mob:guid-a"),
                    new LiveOid(1, 1000, "guid-b", "proto#1000", "proto#1000"),
                    "0x0000000000004321"
                )
            )
        );
        backend.SetResult("quest_state_get", [0x01234567, 0x00000002, 42], CreateNativeRead("quest_state_get", 2));
        var service = new ReadService(backend);

        var snapshot = service.Read(new ReadRequest(CreateSession(), "quest", ["player", "42"]));

        await Assert.That(backend.LocatePlayersCallCount).IsEqualTo(1);
        await Assert.That(backend.InspectCalls).IsEquivalentTo([0x0000000201234567ul]);
        await Assert.That(backend.InvocationCalls).HasSingleItem();
        await Assert.That(backend.InvocationCalls[0].FunctionKey).IsEqualTo("quest_state_get");
        await Assert.That(backend.InvocationCalls[0].StackArguments).IsEquivalentTo([0x01234567u, 0x00000002u, 42u]);
        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.TargetHandleText).IsEqualTo("0x0000000201234567");
        await Assert.That(snapshot.TargetText).Contains("Pc mob:guid-a");
        await Assert
            .That(snapshot.Values.Single(static value => value.Key == "quest_state").ValueText)
            .IsEqualTo("Accepted");
        await Assert
            .That(snapshot.Notes)
            .Contains("Your likely live player is Live PC instance hero (0x0000000201234567).");
    }

    [Test]
    public async Task Read_Field_UsesUnsignedArrayFormattingForSchematicSlots()
    {
        _ = ObjectFieldCatalog.TryGetFieldId("OBJ_F_PC_SCHEMATICS_FOUND_IDX", out var schematicFieldId)
            ? true
            : throw new InvalidOperationException("Expected schematic field id to exist.");
        var backend = new FakeReadBackend();
        backend.SetInspection(0x0000000201234567, CreateIdentity(0x0000000201234567));
        backend.SetResult(
            "obj_array_field_int32_get",
            [0x01234567, 0x00000002, unchecked((uint)schematicFieldId), 0],
            CreateNativeRead("obj_array_field_int32_get", 10079)
        );
        var service = new ReadService(backend);

        var snapshot = service.Read(
            new ReadRequest(
                CreateSession(),
                "field",
                ["0x0000000201234567", "OBJ_F_PC_SCHEMATICS_FOUND_IDX", "0", "unsigned"]
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert
            .That(snapshot.Values.Single(static value => value.Key == "value_text").ValueText)
            .IsEqualTo("Schematic 10079");
        await Assert
            .That(snapshot.Values.Single(static value => value.Key == "storage").ValueText)
            .IsEqualTo("array-uint32");
    }

    [Test]
    public async Task Read_ScriptLocalFlag_ParsesNamedAttachmentPoints()
    {
        var backend = new FakeReadBackend
        {
            PlayerResolution = new LivePlayerLocatorResult(
                0x0000000201234567,
                "SingleLivePcInstance",
                "Resolved one player.",
                [],
                [],
                []
            ),
        };
        backend.SetInspection(0x0000000201234567, CreateIdentity(0x0000000201234567));
        backend.SetResult(
            "script_local_flag_get",
            [0x01234567, 0x00000002, 9, 7],
            CreateNativeRead("script_local_flag_get", 1)
        );
        var service = new ReadService(backend);

        var snapshot = service.Read(new ReadRequest(CreateSession(), "script-local-flag", ["player", "dialog", "7"]));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert
            .That(snapshot.Values.Single(static value => value.Key == "attachment_point").ValueText)
            .IsEqualTo("9");
        await Assert.That(snapshot.Values.Single(static value => value.Key == "enabled").ValueText).IsEqualTo("Yes");
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

    private sealed class FakeReadBackend : IReadBackend
    {
        private readonly Dictionary<string, NativeReadSnapshot> _results = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<ulong, LiveObjectIdentity> _inspections = [];

        public LivePlayerLocatorResult PlayerResolution { get; init; }
        public int LocatePlayersCallCount { get; private set; }
        public List<ulong> InspectCalls { get; } = [];
        public List<InvocationCall> InvocationCalls { get; } = [];

        public void SetInspection(ulong handle, LiveObjectIdentity identity) => _inspections[handle] = identity;

        public void SetResult(string functionKey, IReadOnlyList<uint> stackArguments, NativeReadSnapshot result) =>
            _results[CreateCallKey(functionKey, stackArguments)] = result;

        public LivePlayerLocatorResult LocatePlayers(int processId)
        {
            LocatePlayersCallCount++;
            return PlayerResolution;
        }

        public LiveObjectIdentity InspectHandle(int processId, ulong handle)
        {
            InspectCalls.Add(handle);
            return _inspections.TryGetValue(handle, out var identity)
                ? identity
                : new LiveObjectIdentity(
                    RuntimeSemanticCatalog.FormatHandle(handle),
                    LooksLikeHandle: true,
                    "HandleOnly",
                    PoolIndex: null,
                    BucketIndex: null,
                    SlotIndex: null,
                    EntryAddress: null,
                    ObjectAddress: null,
                    Status: null,
                    Sequence: null,
                    ExpectedSequence: null,
                    Header: null
                );
        }

        public NativeReadSnapshot InvokeInt32(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            string functionKey,
            IReadOnlyList<uint> stackArguments,
            TimeSpan timeout
        )
        {
            InvocationCalls.Add(new InvocationCall(functionKey, [.. stackArguments]));
            return _results[CreateCallKey(functionKey, stackArguments)];
        }

        private static string CreateCallKey(string functionKey, IReadOnlyList<uint> stackArguments) =>
            $"{functionKey}:{string.Join(",", stackArguments)}";
    }

    private readonly record struct InvocationCall(string FunctionKey, IReadOnlyList<uint> StackArguments);
}
