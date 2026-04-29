using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor.Tests;

public sealed class SaveGameValidatorTests
{
    private static readonly GameObjectGuid s_protoId = new(1, 0, 0, Guid.Empty);
    private static readonly GameObjectGuid s_pcObjectId = new(2, 0, 1, Guid.Empty);

    [Test]
    public async Task ValidateMob_PcWithoutRequiredFields_ReturnsWarnings()
    {
        var mob = new CharacterBuilder(ObjectType.Pc, s_pcObjectId, s_protoId).Build();

        var issues = SaveGameValidator.ValidateMob("maps/map01/mobile/G_pc.mob", mob);

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert
            .That(issues.Any(issue => issue.Message.Contains("missing ObjFLocation", StringComparison.Ordinal)))
            .IsTrue();
        await Assert
            .That(issues.Any(issue => issue.Message.Contains("missing ObjFHpPts", StringComparison.Ordinal)))
            .IsTrue();
    }

    [Test]
    public async Task ValidateMobileMd_DuplicateOpaqueRecords_ReturnsWarningAndInfo()
    {
        var recordId = new GameObjectGuid(3, 0, 1, Guid.Empty);
        var md = new MobileMdFile
        {
            Records =
            [
                new MobileMdRecord
                {
                    MapObjectId = recordId,
                    Version = 0x08,
                    RawMobBytes = [],
                },
                new MobileMdRecord
                {
                    MapObjectId = recordId,
                    Version = 0x08,
                    RawMobBytes = [],
                },
            ],
        };

        var issues = SaveGameValidator.ValidateMobileMd("maps/map01/mobile.md", md);

        await Assert.That(issues.Count).IsEqualTo(3);
        await Assert.That(issues.Count(issue => issue.Severity == SaveValidationSeverity.Info)).IsEqualTo(2);
        await Assert
            .That(issues.Any(issue => issue.Message.Contains("duplicate MapObjectId", StringComparison.Ordinal)))
            .IsTrue();
    }

    [Test]
    public async Task ValidateMobileMdy_DuplicateMobObjectIds_ReturnsWarning()
    {
        var npcObjectId = new GameObjectGuid(4, 0, 1, Guid.Empty);
        var firstMob = new CharacterBuilder(ObjectType.Npc, npcObjectId, s_protoId).WithHitPoints(50).Build();
        var secondMob = new CharacterBuilder(ObjectType.Npc, npcObjectId, s_protoId).WithHitPoints(75).Build();
        var mdy = new MobileMdyFile
        {
            Records = [MobileMdyRecord.FromMob(firstMob), MobileMdyRecord.FromMob(secondMob)],
        };

        var issues = SaveGameValidator.ValidateMobileMdy("maps/map01/mobile.mdy", mdy);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Message.Contains("duplicate ObjectId", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Validate_AggregatesTopLevelAndEmbeddedFileFindings()
    {
        const string mdyPath = "maps/map01/mobile.mdy";
        var npcObjectId = new GameObjectGuid(5, 0, 1, Guid.Empty);
        var mdy = new MobileMdyFile
        {
            Records =
            [
                MobileMdyRecord.FromMob(new CharacterBuilder(ObjectType.Npc, npcObjectId, s_protoId).Build()),
                MobileMdyRecord.FromMob(new CharacterBuilder(ObjectType.Npc, npcObjectId, s_protoId).Build()),
            ],
        };
        var save = CreateLoadedSave(
            new SaveInfo
            {
                ModuleName = "Arcanum",
                LeaderName = "",
                DisplayName = "Test",
                MapId = -1,
                GameTimeDays = 1,
                GameTimeMs = 2,
                LeaderPortraitId = 3,
                LeaderLevel = 10,
                LeaderTileX = 4,
                LeaderTileY = 5,
                StoryState = 0,
            },
            mobileMdys: new Dictionary<string, MobileMdyFile>(StringComparer.OrdinalIgnoreCase) { [mdyPath] = mdy }
        );

        var issues = SaveGameValidator.Validate(save);

        await Assert
            .That(
                issues.Any(issue =>
                    issue.FilePath is null && issue.Message.Contains("LeaderName", StringComparison.Ordinal)
                )
            )
            .IsTrue();
        await Assert
            .That(
                issues.Any(issue => issue.FilePath is null && issue.Message.Contains("MapId", StringComparison.Ordinal))
            )
            .IsTrue();
        await Assert
            .That(
                issues.Any(issue =>
                    issue.FilePath == mdyPath && issue.Message.Contains("duplicate ObjectId", StringComparison.Ordinal)
                )
            )
            .IsTrue();
    }

    private static LoadedSave CreateLoadedSave(
        SaveInfo info,
        IReadOnlyDictionary<string, MobData>? mobiles = null,
        IReadOnlyDictionary<string, MobileMdFile>? mobileMds = null,
        IReadOnlyDictionary<string, MobileMdyFile>? mobileMdys = null
    ) =>
        new()
        {
            Info = info,
            Index = new SaveIndex { Root = [] },
            Files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase),
            RawFiles = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase),
            Mobiles = mobiles ?? new Dictionary<string, MobData>(StringComparer.OrdinalIgnoreCase),
            Sectors = new Dictionary<string, Sector>(StringComparer.OrdinalIgnoreCase),
            JumpFiles = new Dictionary<string, JmpFile>(StringComparer.OrdinalIgnoreCase),
            MapPropertiesList = new Dictionary<string, MapProperties>(StringComparer.OrdinalIgnoreCase),
            Messages = new Dictionary<string, MesFile>(StringComparer.OrdinalIgnoreCase),
            TownMapFogs = new Dictionary<string, TownMapFog>(StringComparer.OrdinalIgnoreCase),
            DataSavFiles = new Dictionary<string, DataSavFile>(StringComparer.OrdinalIgnoreCase),
            Data2SavFiles = new Dictionary<string, Data2SavFile>(StringComparer.OrdinalIgnoreCase),
            Scripts = new Dictionary<string, ScrFile>(StringComparer.OrdinalIgnoreCase),
            Dialogs = new Dictionary<string, DlgFile>(StringComparer.OrdinalIgnoreCase),
            MobileMds = mobileMds ?? new Dictionary<string, MobileMdFile>(StringComparer.OrdinalIgnoreCase),
            MobileMdys = mobileMdys ?? new Dictionary<string, MobileMdyFile>(StringComparer.OrdinalIgnoreCase),
            ParseErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        };
}
