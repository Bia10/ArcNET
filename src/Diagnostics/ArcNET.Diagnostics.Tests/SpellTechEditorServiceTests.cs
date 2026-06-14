using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Tests;

public sealed class SpellTechEditorServiceTests
{
    [Test]
    public async Task AddSpell_WhenAliasIsValid_ParsesSpellAndFormatsSnapshot()
    {
        var playerHandle = 0x0000000201234562UL;
        var backend = new FakeSpellTechEditorBackend
        {
            MutationResult = new SpellTechMutationExecutionResult(
                "main-thread-hook",
                "tig_window_display @ tig_window_display (Arcanum.exe+0x0011CF40)",
                "spell_college_level_get @ Arcanum.exe+0x000B1AB0 · stat_base_get @ Arcanum.exe+0x000B0740 · spell_add @ Arcanum.exe+0x000B1790",
                "Conveyance 0 -> 5 · magick points 0 -> 5 · EAX 0x00000001 (1)"
            ),
        };
        backend.Identities[playerHandle] = CreateIdentity(playerHandle, "Pc", "hero", "proto#1000");
        var service = new SpellTechEditorService(backend);

        var snapshot = service.AddSpell(new SpellAddRequest(CreateSession(), "player", "teleportation", "1500"));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.AddSpellHandle).IsEqualTo(playerHandle);
        await Assert.That(backend.AddSpellId).IsEqualTo(4);
        await Assert.That(snapshot.Status).IsEqualTo("Spell added");
        await Assert.That(snapshot.SubjectText).Contains("Teleportation");
        await Assert.That(snapshot.SubjectText).Contains("Conveyance");
        await Assert.That(snapshot.TargetText).Contains("Pc hero");
    }

    [Test]
    public async Task GrantSchematic_WhenAlreadyKnown_FormatsNoMutationStatus()
    {
        var playerHandle = 0x0000000201234562UL;
        var backend = new FakeSpellTechEditorBackend
        {
            MutationResult = new SpellTechMutationExecutionResult(
                "main-thread-hook",
                "tig_window_display @ tig_window_display (Arcanum.exe+0x0011CF40)",
                "obj_array_field_length_get @ Arcanum.exe+0x000079C0 · obj_array_field_int32_get @ Arcanum.exe+0x000072D0",
                "Schematic 10079 is already stored at slot 3 on 0x0000000201234562.",
                NoMutation: true,
                RelatedIndex: 3
            ),
        };
        backend.Identities[playerHandle] = CreateIdentity(playerHandle, "Pc", "hero", "proto#1000");
        var service = new SpellTechEditorService(backend);

        var snapshot = service.GrantSchematic(new SchematicGrantRequest(CreateSession(), "player", "10079", "1000"));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.GrantSchematicId).IsEqualTo(10079);
        await Assert.That(snapshot.Status).IsEqualTo("Schematic already known");
        await Assert.That(snapshot.Summary).Contains("Slot 3");
    }

    [Test]
    public async Task RemoveSchematic_WhenKnown_FormatsRemovalStatus()
    {
        var playerHandle = 0x0000000201234562UL;
        var backend = new FakeSpellTechEditorBackend
        {
            MutationResult = new SpellTechMutationExecutionResult(
                "main-thread-hook",
                "tig_window_display @ tig_window_display (Arcanum.exe+0x0011CF40)",
                "obj_array_field_length_get @ Arcanum.exe+0x000079C0 · obj_array_field_int32_get @ Arcanum.exe+0x000072D0 · obj_array_field_uint32_set @ Arcanum.exe+0x000076B0 · obj_array_field_length_set @ Arcanum.exe+0x00007A20",
                "Removed schematic 10079 from slot 3 on 0x0000000201234562 · length 5 -> 4",
                RelatedIndex: 3
            ),
        };
        backend.Identities[playerHandle] = CreateIdentity(playerHandle, "Pc", "hero", "proto#1000");
        var service = new SpellTechEditorService(backend);

        var snapshot = service.RemoveSchematic(new SchematicRemoveRequest(CreateSession(), "player", "10079", "1000"));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.RemoveSchematicId).IsEqualTo(10079);
        await Assert.That(snapshot.Status).IsEqualTo("Schematic removed");
        await Assert.That(snapshot.Summary).Contains("Slot 3");
    }

    [Test]
    public async Task SetSpellCollegeLevel_WhenCollegeNameIsValid_UsesCollegeId()
    {
        var playerHandle = 0x0000000201234562UL;
        var backend = new FakeSpellTechEditorBackend();
        backend.Identities[playerHandle] = CreateIdentity(playerHandle, "Pc", "hero", "proto#1000");
        var service = new SpellTechEditorService(backend);

        var snapshot = service.SetSpellCollegeLevel(
            new SpellCollegeWriteRequest(CreateSession(), "player", "fire", "3", "1000")
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.SpellCollegeHandle).IsEqualTo(playerHandle);
        await Assert.That(backend.SpellCollegeId).IsEqualTo(4);
        await Assert.That(backend.SpellCollegeLevel).IsEqualTo(3);
        await Assert.That(snapshot.SubjectText).IsEqualTo("Fire");
    }

    [Test]
    public async Task SetTechDisciplineLevel_WhenAliasIsValid_UsesDisciplineId()
    {
        var playerHandle = 0x0000000201234562UL;
        var backend = new FakeSpellTechEditorBackend();
        backend.Identities[playerHandle] = CreateIdentity(playerHandle, "Pc", "hero", "proto#1000");
        var service = new SpellTechEditorService(backend);

        var snapshot = service.SetTechDisciplineLevel(
            new TechDisciplineWriteRequest(CreateSession(), "player", "mechanical", "4", "1000")
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.TechDisciplineHandle).IsEqualTo(playerHandle);
        await Assert.That(backend.TechDisciplineId).IsEqualTo(5);
        await Assert.That(backend.TechDisciplineLevel).IsEqualTo(4);
        await Assert.That(snapshot.SubjectText).IsEqualTo("Mechanical");
    }

    [Test]
    public async Task SetTechSkillPoints_WhenAliasIsValid_UsesSkillId()
    {
        var playerHandle = 0x0000000201234562UL;
        var backend = new FakeSpellTechEditorBackend();
        backend.Identities[playerHandle] = CreateIdentity(playerHandle, "Pc", "hero", "proto#1000");
        var service = new SpellTechEditorService(backend);

        var snapshot = service.SetTechSkillPoints(
            new TechSkillWriteRequest(CreateSession(), "player", "repair", "6", "1000")
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.TechSkillHandle).IsEqualTo(playerHandle);
        await Assert.That(backend.TechSkillId).IsEqualTo(0);
        await Assert.That(backend.TechSkillPoints).IsEqualTo(6);
        await Assert.That(snapshot.SubjectText).IsEqualTo("Repair");
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

    private sealed class FakeSpellTechEditorBackend : ISpellTechEditorBackend
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

        public SpellTechMutationExecutionResult MutationResult { get; init; } =
            new("dispatcher", "dispatcher-site", "detail", "result");

        public ulong AddSpellHandle { get; private set; }
        public int AddSpellId { get; private set; }
        public ulong GrantSchematicHandle { get; private set; }
        public int GrantSchematicId { get; private set; }
        public ulong RemoveSchematicHandle { get; private set; }
        public int RemoveSchematicId { get; private set; }
        public ulong SpellCollegeHandle { get; private set; }
        public int SpellCollegeId { get; private set; }
        public int SpellCollegeLevel { get; private set; }
        public ulong TechDisciplineHandle { get; private set; }
        public int TechDisciplineId { get; private set; }
        public int TechDisciplineLevel { get; private set; }
        public ulong TechSkillHandle { get; private set; }
        public int TechSkillId { get; private set; }
        public int TechSkillPoints { get; private set; }

        public LivePlayerLocatorResult LocatePlayers(int processId) => PlayerResolution;

        public LiveObjectIdentity InspectHandle(int processId, ulong handle) =>
            Identities.TryGetValue(handle, out var identity) ? identity : default;

        public SpellTechMutationExecutionResult AddSpell(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int spellId,
            TimeSpan timeout
        )
        {
            AddSpellHandle = handle;
            AddSpellId = spellId;
            return MutationResult;
        }

        public SpellTechMutationExecutionResult GrantSchematic(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int schematicId,
            TimeSpan timeout
        )
        {
            GrantSchematicHandle = handle;
            GrantSchematicId = schematicId;
            return MutationResult;
        }

        public SpellTechMutationExecutionResult RemoveSchematic(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int schematicId,
            TimeSpan timeout
        )
        {
            RemoveSchematicHandle = handle;
            RemoveSchematicId = schematicId;
            return MutationResult;
        }

        public SpellTechMutationExecutionResult SetSpellCollegeLevel(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int collegeId,
            int level,
            TimeSpan timeout
        )
        {
            SpellCollegeHandle = handle;
            SpellCollegeId = collegeId;
            SpellCollegeLevel = level;
            return MutationResult;
        }

        public SpellTechMutationExecutionResult SetTechDisciplineLevel(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int disciplineId,
            int level,
            TimeSpan timeout
        )
        {
            TechDisciplineHandle = handle;
            TechDisciplineId = disciplineId;
            TechDisciplineLevel = level;
            return MutationResult;
        }

        public SpellTechMutationExecutionResult SetTechSkillPoints(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int skillId,
            int points,
            TimeSpan timeout
        )
        {
            TechSkillHandle = handle;
            TechSkillId = skillId;
            TechSkillPoints = points;
            return MutationResult;
        }
    }
}
