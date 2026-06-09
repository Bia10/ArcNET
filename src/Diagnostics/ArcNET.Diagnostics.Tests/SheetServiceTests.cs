using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Tests;

public sealed class SheetServiceTests
{
    [Test]
    public async Task Read_WhenSessionCannotInvokeFunctions_ReturnsUnavailableSnapshot()
    {
        var service = new SheetService(new FakeSheetBackend());

        var snapshot = service.Read(
            new SheetRequest(
                CreateSession() with
                {
                    Capabilities = new RuntimeCapabilityReport(
                        RuntimeSupportLevel.Exploratory,
                        DiagnosticsCapability.ReadMemory,
                        []
                    ),
                },
                "player",
                "strength"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Sheet read unavailable");
    }

    [Test]
    public async Task Read_UsesSheetLabelAliasesAndPlayerResolution()
    {
        var backend = CreateBackendWithPlayer();
        var service = new SheetService(backend);

        var snapshot = service.Read(new SheetRequest(CreateSession(), "player", "haggle"));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Route).IsEqualTo(SheetRoute.BasicSkill);
        await Assert.That(snapshot.SheetLabel).IsEqualTo("Haggle");
        await Assert.That(snapshot.TargetHandleText).IsEqualTo("0x0000000201234567");
        await Assert.That(snapshot.Values.Single(static value => value.Key == "value").ValueText).IsEqualTo("5");
        await Assert
            .That(snapshot.Values.Single(static value => value.Key == "training_name").ValueText)
            .IsEqualTo("Master");
        await Assert.That(backend.LocatePlayersCallCount).IsEqualTo(1);
    }

    [Test]
    public async Task Scan_ReturnsFullStructuredSheetData()
    {
        var backend = CreateBackendWithPlayer();
        var service = new SheetService(backend);

        var snapshot = service.Scan(new SheetScanRequest(CreateSession(), "player"));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Data.PrimaryStats).Count().IsEqualTo(8);
        await Assert.That(snapshot.Data.Progression).Count().IsEqualTo(7);
        await Assert
            .That(snapshot.Data.BasicSkills.Single(static entry => entry.Name == "Haggle").TrainingName)
            .IsEqualTo("Master");
        await Assert.That(snapshot.Data.SpellMastery.Value).IsEqualTo(4);
        await Assert
            .That(snapshot.Data.TechDisciplines.Single(static entry => entry.Name == "Mechanical").Value)
            .IsEqualTo(3);
    }

    [Test]
    public async Task Diff_ReportsSkillTrainingAndScalarChanges()
    {
        var backend = CreateBackendWithPlayer();
        backend.NextSheetData = CreateChangedData();
        var service = new SheetService(backend);

        var snapshot = service.Diff(new SheetDiffRequest(CreateSession(), "player", 0));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Changed).IsTrue();
        await Assert.That(snapshot.Changes.Select(static change => change.Category)).Contains("PrimaryStat");
        await Assert.That(snapshot.Changes.Select(static change => change.Category)).Contains("BasicSkill");
        await Assert
            .That(snapshot.Changes.Single(static change => change.Category == "BasicSkill").Detail)
            .IsEqualTo("Master->Expert");
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

    private static FakeSheetBackend CreateBackendWithPlayer()
    {
        var backend = new FakeSheetBackend
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

    private static SheetDataSnapshot CreateBaseData() =>
        new(
            PrimaryStats:
            [
                new(0, "Strength", 12),
                new(1, "Dexterity", 10),
                new(2, "Constitution", 9),
                new(3, "Beauty", 8),
                new(4, "Intelligence", 11),
                new(5, "Perception", 10),
                new(6, "Willpower", 9),
                new(7, "Charisma", 8),
            ],
            Progression:
            [
                new(17, "Level", 5),
                new(18, "ExperiencePoints", 1_200),
                new(19, "Alignment", 25),
                new(20, "FatePoints", 2),
                new(21, "UnspentPoints", 1),
                new(22, "MagickPoints", 8),
                new(23, "TechPoints", 4),
            ],
            DerivedStats:
            [
                new(8, "CarryWeight", 105),
                new(9, "DamageBonus", 3),
                new(10, "AcAdjustment", 2),
                new(11, "Speed", 10),
                new(12, "HealRate", 3),
                new(13, "PoisonRecovery", 2),
                new(14, "ReactionModifier", 1),
                new(15, "MaxFollowers", 2),
                new(16, "MagickTechAptitude", 0),
            ],
            Resistances:
            [
                new(0, "Normal", 5),
                new(1, "Fire", 7),
                new(2, "Electrical", 2),
                new(3, "Poison", 4),
                new(4, "Magic", 3),
            ],
            BasicSkills:
            [
                new(0, "Bow", 3, 1, "Apprentice", 67),
                new(1, "Dodge", 4, 1, "Apprentice", 68),
                new(2, "Melee", 4, 1, "Apprentice", 68),
                new(3, "Throw", 2, 0, "Untrained", 2),
                new(4, "Backstab", 1, 0, "Untrained", 1),
                new(5, "Pick Pocket", 1, 0, "Untrained", 1),
                new(6, "Prowling", 2, 0, "Untrained", 2),
                new(7, "Spot Trap", 2, 0, "Untrained", 2),
                new(8, "Gambling", 1, 0, "Untrained", 1),
                new(9, "Haggle", 5, 3, "Master", 197),
                new(10, "Heal", 3, 1, "Apprentice", 67),
                new(11, "Persuasion", 4, 2, "Expert", 132),
            ],
            TechSkills:
            [
                new(0, "Repair", 3, 1, "Apprentice", 67),
                new(1, "Firearms", 2, 0, "Untrained", 2),
                new(2, "Pick Locks", 2, 0, "Untrained", 2),
                new(3, "Disarm Traps", 1, 0, "Untrained", 1),
            ],
            SpellColleges:
            [
                new(0, "Conveyance", 0),
                new(1, "Divination", 0),
                new(2, "Air", 0),
                new(3, "Earth", 1),
                new(4, "Fire", 2),
                new(5, "Water", 0),
                new(6, "Force", 0),
                new(7, "Mental", 0),
                new(8, "Meta", 0),
                new(9, "Morph", 0),
                new(10, "Nature", 1),
                new(11, "Necromantic Black", 0),
                new(12, "Necromantic White", 0),
                new(13, "Phantasm", 0),
                new(14, "Summoning", 0),
                new(15, "Temporal", 0),
            ],
            SpellMastery: new(16, "Spell Mastery", 4),
            TechDisciplines:
            [
                new(0, "Herbology", 0),
                new(1, "Chemistry", 1),
                new(2, "Electric", 2),
                new(3, "Explosives", 0),
                new(4, "Gun Smithy", 0),
                new(5, "Mechanical", 3),
                new(6, "Smithy", 1),
                new(7, "Therapeutics", 0),
            ]
        );

    private static SheetDataSnapshot CreateChangedData()
    {
        var baseData = CreateBaseData();
        return baseData with
        {
            PrimaryStats = [new(0, "Strength", 13), .. baseData.PrimaryStats.Skip(1)],
            BasicSkills =
            [
                .. baseData.BasicSkills.Take(9),
                new(9, "Haggle", 6, 2, "Expert", 134),
                .. baseData.BasicSkills.Skip(10),
            ],
        };
    }

    private sealed class FakeSheetBackend : ISheetBackend
    {
        private readonly Dictionary<ulong, LiveObjectIdentity> _inspections = [];
        private int _readCount;

        public LivePlayerLocatorResult PlayerResolution { get; init; }
        public SheetDataSnapshot SheetData { get; init; } = CreateBaseData();
        public SheetDataSnapshot? NextSheetData { get; set; }
        public int LocatePlayersCallCount { get; private set; }

        public void SetInspection(ulong handle, LiveObjectIdentity identity) => _inspections[handle] = identity;

        public LivePlayerLocatorResult LocatePlayers(int processId)
        {
            LocatePlayersCallCount++;
            return PlayerResolution;
        }

        public LiveObjectIdentity InspectHandle(int processId, ulong handle) => _inspections[handle];

        public SheetDataSnapshot ReadSheetData(int processId, RuntimeProfileSnapshot runtimeProfile, ulong handle)
        {
            _readCount++;
            if (_readCount > 1 && NextSheetData is { } nextData)
            {
                NextSheetData = null;
                return nextData;
            }

            return SheetData;
        }
    }
}
