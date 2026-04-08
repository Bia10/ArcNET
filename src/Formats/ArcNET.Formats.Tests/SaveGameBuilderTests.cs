namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="SaveGameBuilder"/>.</summary>
public sealed class SaveGameBuilderTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CharacterMdyRecord MakePc(string name = "Hero", int gold = 0, int portrait = 0)
    {
        var stats = new int[28];
        return CharacterMdyRecordBuilder.Create(
            stats,
            new int[12],
            new int[4],
            new int[25],
            gold: gold,
            name: name,
            portraitIndex: portrait
        );
    }

    private static SaveInfo MakeInfo(string leaderName = "Hero") =>
        new()
        {
            ModuleName = "Arcanum",
            LeaderName = leaderName,
            DisplayName = "New Game",
            MapId = 1,
            GameTimeDays = 0,
            GameTimeMs = 0,
            LeaderPortraitId = 0,
            LeaderLevel = 1,
            LeaderTileX = 1800,
            LeaderTileY = 940,
            StoryState = 0,
        };

    // ── CreateNew(info, mapPath, pc) ──────────────────────────────────────────

    [Test]
    public async Task CreateNew_WithPc_ReturnsSaveGame()
    {
        var pc = MakePc();
        var info = MakeInfo();

        var save = SaveGameBuilder.CreateNew(info, "modules/Arcanum/maps/Map01", pc);

        await Assert.That(save).IsNotNull();
    }

    [Test]
    public async Task CreateNew_InfoPreserved()
    {
        var info = MakeInfo("Percival");
        var save = SaveGameBuilder.CreateNew(info, "modules/Arcanum/maps/Map01", MakePc());

        await Assert.That(save.Info.LeaderName).IsEqualTo("Percival");
        await Assert.That(save.Info.ModuleName).IsEqualTo("Arcanum");
    }

    [Test]
    public async Task CreateNew_HasOneMap()
    {
        var save = SaveGameBuilder.CreateNew(MakeInfo(), "modules/Arcanum/maps/Map01", MakePc());

        await Assert.That(save.Maps.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CreateNew_MapPathPreserved()
    {
        var save = SaveGameBuilder.CreateNew(MakeInfo(), "modules/Arcanum/maps/Map01", MakePc());

        await Assert.That(save.Maps[0].MapPath).IsEqualTo("modules/Arcanum/maps/Map01");
    }

    [Test]
    public async Task CreateNew_HasPcInDynamicObjects()
    {
        var pc = MakePc("Vesper");
        var save = SaveGameBuilder.CreateNew(MakeInfo(), "modules/Arcanum/maps/Map01", pc);

        var mdy = save.Maps[0].DynamicObjects;
        await Assert.That(mdy).IsNotNull();
        await Assert.That(mdy!.Characters.Count()).IsEqualTo(1);
        await Assert.That(mdy.Characters.First().Name).IsEqualTo("Vesper");
    }

    [Test]
    public async Task CreateNew_StaticObjectsEmpty()
    {
        var save = SaveGameBuilder.CreateNew(MakeInfo(), "modules/Arcanum/maps/Map01", MakePc());

        await Assert.That(save.Maps[0].StaticObjects.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CreateNew_SectorsEmpty()
    {
        var save = SaveGameBuilder.CreateNew(MakeInfo(), "modules/Arcanum/maps/Map01", MakePc());

        await Assert.That(save.Maps[0].Sectors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CreateNew_InvalidMapPath_Throws()
    {
        await Assert
            .That(() => SaveGameBuilder.CreateNew(MakeInfo(), "invalid/path/Map01", MakePc()))
            .ThrowsException()
            .WithMessageContaining("modules/");
    }

    // ── CreateNew(info, map) ──────────────────────────────────────────────────

    [Test]
    public async Task CreateNew_WithMapState_ReturnsSaveGame()
    {
        var map = new SaveMapState
        {
            MapPath = "modules/Arcanum/maps/Map01",
            Sectors = [],
            StaticObjects = [],
        };
        var save = SaveGameBuilder.CreateNew(MakeInfo(), map);

        await Assert.That(save).IsNotNull();
        await Assert.That(save.Maps[0].MapPath).IsEqualTo("modules/Arcanum/maps/Map01");
    }

    [Test]
    public async Task CreateNew_WithMapState_InvalidPath_Throws()
    {
        var map = new SaveMapState
        {
            MapPath = "bad/path/Map01",
            Sectors = [],
            StaticObjects = [],
        };

        await Assert
            .That(() => SaveGameBuilder.CreateNew(MakeInfo(), map))
            .ThrowsException()
            .WithMessageContaining("modules/");
    }

    // ── Round-trip: SaveGameWriter → SaveGameReader ───────────────────────────

    [Test]
    public async Task CreateNew_RoundTrips_ThroughWriter()
    {
        var pc = MakePc("Elsbeth", gold: 250, portrait: 4);
        var info = new SaveInfo
        {
            ModuleName = "Arcanum",
            LeaderName = "Elsbeth",
            DisplayName = "Test Save",
            MapId = 2,
            GameTimeDays = 0,
            GameTimeMs = 0,
            LeaderPortraitId = 4,
            LeaderLevel = 1,
            LeaderTileX = 0,
            LeaderTileY = 0,
            StoryState = 0,
        };

        var save = SaveGameBuilder.CreateNew(info, "modules/Arcanum/maps/Map01", pc);
        var (tfai, tfaf, gsi) = SaveGameWriter.SaveToMemory(save);

        var reparsed = SaveGameReader.ParseMemory(tfai, tfaf, gsi);

        await Assert.That(reparsed.Info.LeaderName).IsEqualTo("Elsbeth");
        await Assert.That(reparsed.Maps.Count).IsEqualTo(1);
        await Assert.That(reparsed.Maps[0].DynamicObjects).IsNotNull();
        var character = reparsed.Maps[0].DynamicObjects!.Characters.FirstOrDefault();
        await Assert.That(character).IsNotNull();
        await Assert.That(character!.Name).IsEqualTo("Elsbeth");
        await Assert.That(character.Gold).IsEqualTo(250);
        await Assert.That(character.PortraitIndex).IsEqualTo(4);
    }
}
