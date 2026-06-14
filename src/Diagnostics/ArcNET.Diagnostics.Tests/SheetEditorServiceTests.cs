using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Tests;

public sealed class SheetEditorServiceTests
{
    [Test]
    public async Task Write_WhenStatAliasIsValid_RoutesToStatMutation()
    {
        var playerHandle = 0x0000000201234562UL;
        var backend = new FakeSheetEditorBackend();
        backend.Identities[playerHandle] = CreateIdentity(playerHandle, "Pc", "hero", "proto#1000");
        var service = new SheetEditorService(backend);

        var snapshot = service.Write(new SheetWriteRequest(CreateSession(), "player", "strength", "13", "", "1000"));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Status).IsEqualTo("Sheet field updated");
        await Assert.That(backend.StatId).IsEqualTo(0);
        await Assert.That(backend.StatValue).IsEqualTo(13);
        await Assert.That(snapshot.FieldDisplayName).IsEqualTo("Strength");
    }

    [Test]
    public async Task Write_WhenBasicSkillTrainingIsSpecified_RoutesToSkillMutation()
    {
        var playerHandle = 0x0000000201234562UL;
        var backend = new FakeSheetEditorBackend();
        backend.Identities[playerHandle] = CreateIdentity(playerHandle, "Pc", "hero", "proto#1000");
        var service = new SheetEditorService(backend);

        var snapshot = service.Write(new SheetWriteRequest(CreateSession(), "player", "haggle", "6", "expert", "1000"));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.BasicSkillId).IsEqualTo(9);
        await Assert.That(backend.BasicSkillPoints).IsEqualTo(6);
        await Assert.That(backend.BasicSkillTraining).IsEqualTo(2);
        await Assert.That(snapshot.TrainingText).IsEqualTo("expert");
    }

    [Test]
    public async Task Write_WhenSpellMasteryUsesNoneAlias_RoutesToSpellMasteryMutation()
    {
        var playerHandle = 0x0000000201234562UL;
        var backend = new FakeSheetEditorBackend();
        backend.Identities[playerHandle] = CreateIdentity(playerHandle, "Pc", "hero", "proto#1000");
        var service = new SheetEditorService(backend);

        var snapshot = service.Write(
            new SheetWriteRequest(CreateSession(), "player", "spell-mastery", "none", "", "1000")
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.SpellMasteryCollegeId).IsEqualTo(-1);
        await Assert.That(snapshot.FieldDisplayName).IsEqualTo("Spell Mastery");
        await Assert.That(snapshot.Route).IsEqualTo(SheetRoute.SpellMastery);
    }

    [Test]
    public async Task Write_WhenGenderUsesNamedValue_RoutesToStatMutation()
    {
        var playerHandle = 0x0000000201234562UL;
        var backend = new FakeSheetEditorBackend();
        backend.Identities[playerHandle] = CreateIdentity(playerHandle, "Pc", "hero", "proto#1000");
        var service = new SheetEditorService(backend);

        var snapshot = service.Write(new SheetWriteRequest(CreateSession(), "player", "gender", "Female", "", "1000"));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.StatId).IsEqualTo(26);
        await Assert.That(backend.StatValue).IsEqualTo(1);
        await Assert.That(snapshot.FieldDisplayName).IsEqualTo("Gender");
    }

    [Test]
    public async Task Write_WhenRaceUsesNamedValue_RoutesToStatMutation()
    {
        var playerHandle = 0x0000000201234562UL;
        var backend = new FakeSheetEditorBackend();
        backend.Identities[playerHandle] = CreateIdentity(playerHandle, "Pc", "hero", "proto#1000");
        var service = new SheetEditorService(backend);

        var snapshot = service.Write(new SheetWriteRequest(CreateSession(), "player", "race", "Half-Ogre", "", "1000"));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.StatId).IsEqualTo(27);
        await Assert.That(backend.StatValue).IsEqualTo(3);
        await Assert.That(snapshot.FieldDisplayName).IsEqualTo("Race");
    }

    [Test]
    public async Task Write_WhenSessionCannotInvokeFunctions_ReturnsUnavailableSnapshot()
    {
        var service = new SheetEditorService(new FakeSheetEditorBackend());

        var snapshot = service.Write(
            new SheetWriteRequest(
                CreateSession() with
                {
                    Capabilities = new RuntimeCapabilityReport(
                        RuntimeSupportLevel.Exploratory,
                        DiagnosticsCapability.ReadMemory,
                        []
                    ),
                },
                "player",
                "strength",
                "13",
                "",
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Sheet editor unavailable");
    }

    [Test]
    public async Task Write_WhenTechDisciplineDegreeExceedsSeven_ReturnsUnavailableSnapshot()
    {
        var playerHandle = 0x0000000201234562UL;
        var backend = new FakeSheetEditorBackend();
        backend.Identities[playerHandle] = CreateIdentity(playerHandle, "Pc", "hero", "proto#1000");
        var service = new SheetEditorService(backend);

        var snapshot = service.Write(new SheetWriteRequest(CreateSession(), "player", "mechanical", "8", "", "1000"));

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Invalid sheet edit request");
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

    private sealed class FakeSheetEditorBackend : ISheetEditorBackend
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

        public int StatId { get; private set; }
        public int StatValue { get; private set; }
        public int ResistanceId { get; private set; }
        public int ResistanceValue { get; private set; }
        public int BasicSkillId { get; private set; }
        public int BasicSkillPoints { get; private set; }
        public int? BasicSkillTraining { get; private set; }
        public int TechSkillId { get; private set; }
        public int TechSkillPoints { get; private set; }
        public int? TechSkillTraining { get; private set; }
        public int SpellCollegeId { get; private set; }
        public int SpellCollegeLevel { get; private set; }
        public int SpellMasteryCollegeId { get; private set; }
        public int TechDisciplineId { get; private set; }
        public int TechDisciplineLevel { get; private set; }

        public LivePlayerLocatorResult LocatePlayers(int processId) => PlayerResolution;

        public LiveObjectIdentity InspectHandle(int processId, ulong handle) =>
            Identities.TryGetValue(handle, out var identity) ? identity : default;

        public SheetMutationExecutionResult SetStat(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int statId,
            int value,
            TimeSpan timeout
        )
        {
            StatId = statId;
            StatValue = value;
            return new SheetMutationExecutionResult("dispatcher", "dispatcher-site", "detail", "result");
        }

        public SheetMutationExecutionResult SetResistance(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int resistanceId,
            int value,
            TimeSpan timeout
        )
        {
            ResistanceId = resistanceId;
            ResistanceValue = value;
            return new SheetMutationExecutionResult("dispatcher", "dispatcher-site", "detail", "result");
        }

        public SheetMutationExecutionResult SetBasicSkill(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int skillId,
            int points,
            int? training,
            TimeSpan timeout
        )
        {
            BasicSkillId = skillId;
            BasicSkillPoints = points;
            BasicSkillTraining = training;
            return new SheetMutationExecutionResult("dispatcher", "dispatcher-site", "detail", "result");
        }

        public SheetMutationExecutionResult SetTechSkill(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int skillId,
            int points,
            int? training,
            TimeSpan timeout
        )
        {
            TechSkillId = skillId;
            TechSkillPoints = points;
            TechSkillTraining = training;
            return new SheetMutationExecutionResult("dispatcher", "dispatcher-site", "detail", "result");
        }

        public SheetMutationExecutionResult SetSpellCollegeLevel(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int collegeId,
            int level,
            TimeSpan timeout
        )
        {
            SpellCollegeId = collegeId;
            SpellCollegeLevel = level;
            return new SheetMutationExecutionResult("dispatcher", "dispatcher-site", "detail", "result");
        }

        public SheetMutationExecutionResult SetSpellMastery(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int masteryCollegeId,
            TimeSpan timeout
        )
        {
            SpellMasteryCollegeId = masteryCollegeId;
            return new SheetMutationExecutionResult("dispatcher", "dispatcher-site", "detail", "result");
        }

        public SheetMutationExecutionResult SetTechDisciplineLevel(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int disciplineId,
            int level,
            TimeSpan timeout
        )
        {
            TechDisciplineId = disciplineId;
            TechDisciplineLevel = level;
            return new SheetMutationExecutionResult("dispatcher", "dispatcher-site", "detail", "result");
        }
    }
}
