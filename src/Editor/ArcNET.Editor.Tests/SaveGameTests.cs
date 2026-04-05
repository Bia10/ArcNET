using System.Collections;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor.Tests;

public class SaveGameTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static MobData MakePc()
    {
        var protoId = new GameObjectGuid(1, 0, 0, Guid.Empty);
        var objectId = new GameObjectGuid(2, 0, 1, Guid.Empty);
        return new CharacterBuilder(ObjectType.Pc, objectId, protoId)
            .WithPlayerName("TestPlayer")
            .WithHitPoints(80)
            .Build();
    }

    private static (SaveGame save, byte[] tfafBytes) MakeMinimalSave()
    {
        var mobBytes = MobFormat.WriteToArray(MakePc());
        var nonMobBytes = new byte[] { 0xAA, 0xBB, 0xCC };

        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = mobBytes,
            ["maps/map01/map.jmp"] = nonMobBytes,
        };

        var index = new SaveIndex
        {
            Root = new List<TfaiEntry>
            {
                new TfaiDirectoryEntry
                {
                    Name = "maps",
                    Children = new List<TfaiEntry>
                    {
                        new TfaiDirectoryEntry
                        {
                            Name = "map01",
                            Children = new List<TfaiEntry>
                            {
                                new TfaiDirectoryEntry
                                {
                                    Name = "mobile",
                                    Children = new List<TfaiEntry>
                                    {
                                        new TfaiFileEntry { Name = "G_pc.mob", Size = mobBytes.Length },
                                    }.AsReadOnly(),
                                },
                                new TfaiFileEntry { Name = "map.jmp", Size = nonMobBytes.Length },
                            }.AsReadOnly(),
                        },
                    }.AsReadOnly(),
                },
            }.AsReadOnly(),
        };

        var info = new SaveInfo
        {
            ModuleName = "arcanum",
            LeaderName = "TestPlayer",
            DisplayName = "Test Save",
            MapId = 1,
            GameTimeDays = 0,
            GameTimeMs = 0,
            LeaderPortraitId = 1,
            LeaderLevel = 1,
            LeaderTileX = 0,
            LeaderTileY = 0,
            StoryState = 0,
        };

        var tfafBytes = TfafFormat.Pack(index, files);
        var save = SaveGameLoader.LoadFromParsed(info, index, tfafBytes);
        return (save, tfafBytes);
    }

    // ── SaveGameLoader ────────────────────────────────────────────────────────

    [Test]
    public async Task LoadFromParsed_ParsesMobFiles()
    {
        var (save, _) = MakeMinimalSave();
        await Assert.That(save.Mobiles.ContainsKey("maps/map01/mobile/G_pc.mob")).IsTrue();
    }

    [Test]
    public async Task LoadFromParsed_DoesNotParseNonMobFiles()
    {
        var (save, _) = MakeMinimalSave();
        await Assert.That(save.Mobiles.ContainsKey("maps/map01/map.jmp")).IsFalse();
    }

    [Test]
    public async Task LoadFromParsed_PreservesAllFiles()
    {
        var (save, _) = MakeMinimalSave();
        await Assert.That(save.Files.ContainsKey("maps/map01/mobile/G_pc.mob")).IsTrue();
        await Assert.That(save.Files.ContainsKey("maps/map01/map.jmp")).IsTrue();
    }

    [Test]
    public async Task LoadFromParsed_MobDataParsedCorrectly()
    {
        var (save, _) = MakeMinimalSave();
        var mob = save.Mobiles["maps/map01/mobile/G_pc.mob"];
        await Assert.That(mob.Header.GameObjectType).IsEqualTo(ObjectType.Pc);
        var nameProp = mob.Properties.First(p => p.Field == ObjectField.ObjFPcPlayerName);
        await Assert.That(nameProp.GetString()).IsEqualTo("TestPlayer");
    }

    // ── SaveGameWriter.RebuildIndex ───────────────────────────────────────────

    [Test]
    public async Task RebuildIndex_UnchangedFiles_PreservesAllSizes()
    {
        var (save, _) = MakeMinimalSave();
        var updatedFiles = new Dictionary<string, byte[]>(save.Files, StringComparer.OrdinalIgnoreCase);

        var rebuilt = SaveGameWriter.RebuildIndex(save.Index, updatedFiles);

        var mapsDir = (TfaiDirectoryEntry)rebuilt.Root[0];
        var map01Dir = (TfaiDirectoryEntry)mapsDir.Children[0];
        var mobileDir = (TfaiDirectoryEntry)map01Dir.Children[0];
        var mobFile = (TfaiFileEntry)mobileDir.Children[0];
        var jmpFile = (TfaiFileEntry)map01Dir.Children[1];

        await Assert.That(mobFile.Size).IsEqualTo(save.Files["maps/map01/mobile/G_pc.mob"].Length);
        await Assert.That(jmpFile.Size).IsEqualTo(save.Files["maps/map01/map.jmp"].Length);
    }

    [Test]
    public async Task RebuildIndex_UpdatedMob_ReflectsNewSize()
    {
        var (save, _) = MakeMinimalSave();

        var newMob = new CharacterBuilder(save.Mobiles["maps/map01/mobile/G_pc.mob"])
            .WithBaseStats([10, 12, 9, 14, 8, 11])
            .WithGold(500)
            .Build();
        var newMobBytes = MobFormat.WriteToArray(newMob);

        var updatedFiles = new Dictionary<string, byte[]>(save.Files, StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = newMobBytes,
        };

        var rebuilt = SaveGameWriter.RebuildIndex(save.Index, updatedFiles);

        var mapsDir = (TfaiDirectoryEntry)rebuilt.Root[0];
        var map01Dir = (TfaiDirectoryEntry)mapsDir.Children[0];
        var mobileDir = (TfaiDirectoryEntry)map01Dir.Children[0];
        var mobFile = (TfaiFileEntry)mobileDir.Children[0];

        await Assert.That(mobFile.Size).IsEqualTo(newMobBytes.Length);
    }

    // ── SaveGameWriter round-trip (via temp files) ────────────────────────────

    [Test]
    public async Task Save_ThenLoad_RoundTrips()
    {
        var (save, _) = MakeMinimalSave();
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);

        try
        {
            SaveGameWriter.Save(save, tmpDir, "testslot");
            var loaded = SaveGameLoader.Load(tmpDir, "testslot");

            await Assert.That(loaded.Info.LeaderName).IsEqualTo(save.Info.LeaderName);
            await Assert.That(loaded.Mobiles.ContainsKey("maps/map01/mobile/G_pc.mob")).IsTrue();
            var mob = loaded.Mobiles["maps/map01/mobile/G_pc.mob"];
            var nameProp = mob.Properties.First(p => p.Field == ObjectField.ObjFPcPlayerName);
            await Assert.That(nameProp.GetString()).IsEqualTo("TestPlayer");
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_WithUpdatedMobile_PersistsChanges()
    {
        var (save, _) = MakeMinimalSave();
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);

        try
        {
            var updatedMob = new CharacterBuilder(save.Mobiles["maps/map01/mobile/G_pc.mob"])
                .WithPlayerName("Modified")
                .WithGold(999)
                .Build();

            SaveGameWriter.Save(
                save,
                tmpDir,
                "testslot",
                updatedMobiles: new Dictionary<string, MobData> { ["maps/map01/mobile/G_pc.mob"] = updatedMob }
            );

            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            var mob = loaded.Mobiles["maps/map01/mobile/G_pc.mob"];
            var name = mob.Properties.First(p => p.Field == ObjectField.ObjFPcPlayerName);
            await Assert.That(name.GetString()).IsEqualTo("Modified");
            var gold = mob.Properties.First(p => p.Field == ObjectField.ObjFCritterGold);
            await Assert.That(gold.GetInt32()).IsEqualTo(999);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_WithUpdatedInfo_PersistsChanges()
    {
        var (save, _) = MakeMinimalSave();
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);

        try
        {
            var updatedInfo = new SaveInfo
            {
                ModuleName = save.Info.ModuleName,
                LeaderName = "NewLeader",
                DisplayName = "Updated Save",
                MapId = save.Info.MapId,
                GameTimeDays = save.Info.GameTimeDays,
                GameTimeMs = save.Info.GameTimeMs,
                LeaderPortraitId = save.Info.LeaderPortraitId,
                LeaderLevel = 10,
                LeaderTileX = save.Info.LeaderTileX,
                LeaderTileY = save.Info.LeaderTileY,
                StoryState = save.Info.StoryState,
            };

            SaveGameWriter.Save(save, tmpDir, "testslot", updatedInfo: updatedInfo);
            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            await Assert.That(loaded.Info.LeaderName).IsEqualTo("NewLeader");
            await Assert.That(loaded.Info.LeaderLevel).IsEqualTo(10);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }
}
