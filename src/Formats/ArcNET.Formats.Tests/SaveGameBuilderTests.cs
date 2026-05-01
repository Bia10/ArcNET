using System.Buffers.Binary;

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

    private static byte[] BuildData2SavBytes(int startInt = 6, int pairCount = 40)
    {
        var totalInts = startInt + pairCount * 2 + 2;
        var bytes = new byte[totalInts * 4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0, 4), 25);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4, 4), 0);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8, 4), 1);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12, 4), 2);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16, 4), -1);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(20, 4), 0);

        for (var index = 0; index < pairCount; index++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan((startInt + index * 2) * 4, 4), index % 6);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan((startInt + index * 2 + 1) * 4, 4), 50000 + index);
        }

        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan((startInt + pairCount * 2) * 4, 4), 169);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan((startInt + pairCount * 2 + 1) * 4, 4), 186);
        return bytes;
    }

    private static byte[] BuildDataSavBytes()
    {
        var bytes = new byte[50];
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0, 4), 25);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4, 4), 32);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8, 4), 7);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12, 4), 18);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16, 4), 2072);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(20, 4), 0x02441780);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(24, 4), 18);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(28, 4), 25);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(32, 4), 2072);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(36, 4), 0x02559988);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(40, 4), 123);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(44, 4), 456);
        bytes[48] = 0xAA;
        bytes[49] = 0xBB;
        return bytes;
    }

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
    public async Task CreateNew_TopLevelFilesDefaultEmpty()
    {
        var save = SaveGameBuilder.CreateNew(MakeInfo(), "modules/Arcanum/maps/Map01", MakePc());

        await Assert.That(save.TownMapFogs).IsEmpty();
        await Assert.That(save.DataSavFiles).IsEmpty();
        await Assert.That(save.Data2SavFiles).IsEmpty();
        await Assert.That(save.RawFiles).IsEmpty();
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

    [Test]
    public async Task SaveAndLoad_WithDecoratedGsiCompanion_UsesLogicalSlotStem()
    {
        var save = SaveGameBuilder.CreateNew(MakeInfo("Elsbeth"), "modules/Arcanum/maps/Map01", MakePc("Elsbeth"));
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var tfaiPath = Path.Combine(tempDir, "Slot0000.tfai");
            var exactGsiPath = Path.Combine(tempDir, "Slot0000.gsi");
            var decoratedGsiPath = Path.Combine(tempDir, "Slot0000Elsbeth.gsi");

            SaveGameWriter.Save(save, tfaiPath);
            File.Move(exactGsiPath, decoratedGsiPath);

            SaveGameWriter.Save(save, tfaiPath);
            var reparsed = SaveGameReader.Load(tfaiPath);

            await Assert.That(File.Exists(decoratedGsiPath)).IsTrue();
            await Assert.That(File.Exists(exactGsiPath)).IsFalse();
            await Assert.That(reparsed.Info.LeaderName).IsEqualTo("Elsbeth");
            await Assert.That(reparsed.Maps.Count).IsEqualTo(1);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreateNew_RoundTrips_TopLevelSaveGlobalFiles()
    {
        var baseSave = SaveGameBuilder.CreateNew(MakeInfo("Elsbeth"), "modules/Arcanum/maps/Map01", MakePc("Elsbeth"));
        byte[] dataSav = BuildDataSavBytes();
        byte[] data2Sav = BuildData2SavBytes();
        byte[] nestedRaw = [0x01, 0x02, 0x03, 0x04];

        var save = new SaveGame
        {
            Info = baseSave.Info,
            Maps = baseSave.Maps,
            TownMapFogs = [("Tsen Ang.tmf", new TownMapFog { RawBytes = [0x03, 0x80] })],
            DataSavFiles = [("data.sav", DataSavFormat.ParseMemory(dataSav))],
            Data2SavFiles = [("data2.sav", Data2SavFormat.ParseMemory(data2Sav))],
            RawFiles = [("globals/custom.bin", nestedRaw)],
        };

        var (tfai, tfaf, gsi) = SaveGameWriter.SaveToMemory(save);
        var reparsed = SaveGameReader.ParseMemory(tfai, tfaf, gsi);
        var index = SaveIndexFormat.ParseMemory(tfai);

        await Assert.That(reparsed.TownMapFogs.Count).IsEqualTo(1);
        await Assert.That(reparsed.TownMapFogs[0].VirtualPath).IsEqualTo("Tsen Ang.tmf");
        await Assert.That(reparsed.TownMapFogs[0].Data.RawBytes.SequenceEqual(new byte[] { 0x03, 0x80 })).IsTrue();
        await Assert.That(reparsed.DataSavFiles.Count).IsEqualTo(1);
        await Assert.That(reparsed.DataSavFiles[0].VirtualPath).IsEqualTo("data.sav");
        await Assert.That(reparsed.DataSavFiles[0].Data.Header0).IsEqualTo(25);
        await Assert.That(reparsed.DataSavFiles[0].Data.Header1).IsEqualTo(32);
        await Assert
            .That(reparsed.DataSavFiles[0].Data.GetQuadRow(0))
            .IsEqualTo(new DataSavQuadRow(7, 18, 2072, 0x02441780));
        await Assert.That(reparsed.Data2SavFiles.Count).IsEqualTo(1);
        await Assert.That(reparsed.Data2SavFiles[0].VirtualPath).IsEqualTo("data2.sav");
        await Assert.That(reparsed.Data2SavFiles[0].Data.TryGetIdPairValue(50005, out var data2Value)).IsTrue();
        await Assert.That(data2Value).IsEqualTo(5);
        await Assert
            .That(
                reparsed
                    .RawFiles.Single(static f => f.VirtualPath == "globals/custom.bin")
                    .Data.SequenceEqual(nestedRaw)
            )
            .IsTrue();

        await Assert.That(index.Root.OfType<TfaiFileEntry>().Any(static f => f.Name == "Tsen Ang.tmf")).IsTrue();
        await Assert.That(index.Root.OfType<TfaiFileEntry>().Any(static f => f.Name == "data.sav")).IsTrue();
        await Assert.That(index.Root.OfType<TfaiFileEntry>().Any(static f => f.Name == "data2.sav")).IsTrue();
        var globals = index.Root.OfType<TfaiDirectoryEntry>().Single(static d => d.Name == "globals");
        await Assert.That(globals.Children.OfType<TfaiFileEntry>().Any(static f => f.Name == "custom.bin")).IsTrue();
    }

    [Test]
    public async Task ParseMemory_RoundTrips_UnknownMapLocalFiles()
    {
        var save = SaveGameBuilder.CreateNew(MakeInfo("Elsbeth"), "modules/Arcanum/maps/Map01", MakePc("Elsbeth"));
        byte[] unknownPayload = [0xDE, 0xAD, 0xBE, 0xEF, 0x42];

        var initialPayloads = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["modules/Arcanum/maps/Map01/map.jmp"] = JmpFormat.WriteToArray(new JmpFile { Jumps = [] }),
            ["modules/Arcanum/maps/Map01/mobile.mdy"] = MobileMdyFormat.WriteToArray(save.Maps[0].DynamicObjects!),
            ["modules/Arcanum/maps/Map01/state/custom.bin"] = unknownPayload,
        };

        var initialIndex = new SaveIndex
        {
            Root =
            [
                new TfaiDirectoryEntry
                {
                    Name = "modules",
                    Children =
                    [
                        new TfaiDirectoryEntry
                        {
                            Name = "Arcanum",
                            Children =
                            [
                                new TfaiDirectoryEntry
                                {
                                    Name = "maps",
                                    Children =
                                    [
                                        new TfaiDirectoryEntry
                                        {
                                            Name = "Map01",
                                            Children =
                                            [
                                                new TfaiFileEntry
                                                {
                                                    Name = "map.jmp",
                                                    Size = initialPayloads["modules/Arcanum/maps/Map01/map.jmp"].Length,
                                                },
                                                new TfaiFileEntry
                                                {
                                                    Name = "mobile.mdy",
                                                    Size = initialPayloads[
                                                        "modules/Arcanum/maps/Map01/mobile.mdy"
                                                    ].Length,
                                                },
                                                new TfaiDirectoryEntry
                                                {
                                                    Name = "state",
                                                    Children =
                                                    [
                                                        new TfaiFileEntry
                                                        {
                                                            Name = "custom.bin",
                                                            Size = unknownPayload.Length,
                                                        },
                                                    ],
                                                },
                                            ],
                                        },
                                    ],
                                },
                            ],
                        },
                    ],
                },
            ],
        };

        var parsed = SaveGameReader.ParseMemory(
            SaveIndexFormat.WriteToArray(initialIndex),
            TfafFormat.Pack(initialIndex, initialPayloads),
            SaveInfoFormat.WriteToArray(save.Info)
        );

        var (roundTripTfai, roundTripTfaf, _) = SaveGameWriter.SaveToMemory(parsed);
        var roundTripPayloads = TfafFormat.ExtractAll(SaveIndexFormat.ParseMemory(roundTripTfai), roundTripTfaf);

        await Assert.That(roundTripPayloads.ContainsKey("modules/Arcanum/maps/Map01/state/custom.bin")).IsTrue();
        await Assert
            .That(roundTripPayloads["modules/Arcanum/maps/Map01/state/custom.bin"])
            .IsEquivalentTo(unknownPayload);
    }
}
