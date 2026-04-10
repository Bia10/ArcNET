using System.Buffers.Binary;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor.Tests;

public class SaveGameEditorTests
{
    // ── Wire-format helpers ───────────────────────────────────────────────────

    // Builds a minimal SAR packet: presence(1) + elemSz(4) + elemCnt(4) + bsId(4) + data + bsCnt(4)
    private static byte[] Sar(int elemSz, int elemCnt, int bsId, byte[] data)
    {
        var buf = new byte[13 + data.Length + 4];
        buf[0] = 0x01;
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(1, 4), elemSz);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(5, 4), elemCnt);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(9, 4), bsId);
        data.CopyTo(buf, 13);
        // bsCnt = 0 at buf[13 + data.Length] (already zeroed)
        return buf;
    }

    private static byte[] IntArray(int count, int fill = 0)
    {
        var b = new byte[count * 4];
        if (fill != 0)
            for (var i = 0; i < count; i++)
                BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(i * 4, 4), fill);
        return b;
    }

    /// <summary>
    /// Builds a full v2 character record with controllable level, alignment, and gold.
    /// All four primary SAR arrays are present so HasCompleteData is true.
    /// A gold SAR (bsId=0x4B13) is included whenever <paramref name="gold"/> != 0.
    /// </summary>
    private static byte[] BuildV2Record(int level = 5, int alignment = 100, int gold = 0)
    {
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        // Stats: index 17 = level, index 19 = alignment.
        var statsData = new byte[28 * 4];
        BinaryPrimitives.WriteInt32LittleEndian(statsData.AsSpan(17 * 4, 4), level);
        BinaryPrimitives.WriteInt32LittleEndian(statsData.AsSpan(19 * 4, 4), alignment);
        var statsSar = Sar(4, 28, 0x4DA5, statsData);
        var basicSar = Sar(4, 12, 0x43C3, IntArray(12));
        var techSar = Sar(4, 4, 0x4A07, IntArray(4));
        var spellSar = Sar(4, 25, 0x4A08, IntArray(25));

        if (gold != 0)
        {
            var goldData = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(goldData, gold);
            var goldSar = Sar(4, 1, 0x4B13, goldData);
            return [.. magic, .. statsSar, .. basicSar, .. techSar, .. spellSar, .. goldSar];
        }

        return [.. magic, .. statsSar, .. basicSar, .. techSar, .. spellSar];
    }

    /// <summary>
    /// Builds a <see cref="LoadedSave"/> containing a single <c>mobile.mdy</c> with one
    /// v2 PC record.  Returns the save and the virtual path to the mdy file.
    /// </summary>
    private static (LoadedSave save, string mdyPath) MakeSaveWithPc(int level = 5, int alignment = 100, int gold = 0)
    {
        var v2Bytes = BuildV2Record(level, alignment, gold);
        const string mdyPath = "maps/Arcanum1-024/mobile.mdy";

        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase) { [mdyPath] = v2Bytes };

        var index = new SaveIndex
        {
            Root =
            [
                new TfaiDirectoryEntry
                {
                    Name = "maps",
                    Children =
                    [
                        new TfaiDirectoryEntry
                        {
                            Name = "Arcanum1-024",
                            Children = [new TfaiFileEntry { Name = "mobile.mdy", Size = v2Bytes.Length }],
                        },
                    ],
                },
            ],
        };

        var info = new SaveInfo
        {
            ModuleName = "arcanum",
            LeaderName = "TestPC",
            DisplayName = "Editor Test Save",
            MapId = 24,
            GameTimeDays = 0,
            GameTimeMs = 0,
            LeaderPortraitId = 1,
            LeaderLevel = Math.Max(1, level),
            LeaderTileX = 0,
            LeaderTileY = 0,
            StoryState = 0,
        };

        var tfafBytes = TfafFormat.Pack(index, files);
        var save = SaveGameLoader.LoadFromParsed(info, index, tfafBytes);
        return (save, mdyPath);
    }

    /// <summary>
    /// Builds a <see cref="LoadedSave"/> that has only a standard <c>.mob</c> file
    /// and NO <c>mobile.mdy</c> — so TryFindPlayerCharacter should return false.
    /// </summary>
    private static LoadedSave MakeSaveWithoutMdy()
    {
        var protoId = new GameObjectGuid(1, 0, 0, Guid.Empty);
        var objectId = new GameObjectGuid(2, 0, 1, Guid.Empty);
        var mobBytes = MobFormat.WriteToArray(
            new CharacterBuilder(ObjectType.Pc, objectId, protoId).WithPlayerName("NoPc").Build()
        );

        const string mobPath = "maps/map01/mobile/G_pc.mob";
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase) { [mobPath] = mobBytes };

        var index = new SaveIndex
        {
            Root =
            [
                new TfaiDirectoryEntry
                {
                    Name = "maps",
                    Children =
                    [
                        new TfaiDirectoryEntry
                        {
                            Name = "map01",
                            Children =
                            [
                                new TfaiDirectoryEntry
                                {
                                    Name = "mobile",
                                    Children = [new TfaiFileEntry { Name = "G_pc.mob", Size = mobBytes.Length }],
                                },
                            ],
                        },
                    ],
                },
            ],
        };

        var info = new SaveInfo
        {
            ModuleName = "arcanum",
            LeaderName = "NoPc",
            DisplayName = "No MDY Save",
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
        return SaveGameLoader.LoadFromParsed(info, index, tfafBytes);
    }

    // ── TryFindPlayerCharacter ────────────────────────────────────────────────

    [Test]
    public async Task TryFindPlayerCharacter_FindsPcWithCompleteData()
    {
        var (save, _) = MakeSaveWithPc(level: 7, alignment: 55);
        var editor = new SaveGameEditor(save);

        var found = editor.TryFindPlayerCharacter(out var pc, out _);

        await Assert.That(found).IsTrue();
        await Assert.That(pc.Level).IsEqualTo(7);
        await Assert.That(pc.Alignment).IsEqualTo(55);
        await Assert.That(pc.HasCompleteData).IsTrue();
    }

    [Test]
    public async Task TryFindPlayerCharacter_ReturnsFalse_WhenNoMdy()
    {
        var save = MakeSaveWithoutMdy();
        var editor = new SaveGameEditor(save);

        var found = editor.TryFindPlayerCharacter(out _, out _);

        await Assert.That(found).IsFalse();
    }

    [Test]
    public async Task TryFindPlayerCharacter_ReturnsCorrectMdyPath()
    {
        var (save, expectedPath) = MakeSaveWithPc();
        var editor = new SaveGameEditor(save);

        editor.TryFindPlayerCharacter(out _, out var mdyPath);

        await Assert.That(mdyPath).IsEqualTo(expectedPath);
    }

    // ── TryFindCharacter (custom predicate) ───────────────────────────────────

    [Test]
    public async Task TryFindCharacter_CustomPredicate_MatchesByLevel()
    {
        var (save, _) = MakeSaveWithPc(level: 10);
        var editor = new SaveGameEditor(save);

        var found = editor.TryFindCharacter(c => c.Level == 10, out var pc, out _);

        await Assert.That(found).IsTrue();
        await Assert.That(pc.Level).IsEqualTo(10);
    }

    [Test]
    public async Task TryFindCharacter_CustomPredicate_ReturnsFalse_WhenNoMatch()
    {
        var (save, _) = MakeSaveWithPc(level: 5);
        var editor = new SaveGameEditor(save);

        var found = editor.TryFindCharacter(c => c.Level == 99, out _, out _);

        await Assert.That(found).IsFalse();
    }

    [Test]
    public async Task TryFindCharacter_CustomPredicate_MatchesByAlignment()
    {
        var (save, _) = MakeSaveWithPc(alignment: 175);
        var editor = new SaveGameEditor(save);

        var found = editor.TryFindCharacter(c => c.Alignment == 175, out var pc, out _);

        await Assert.That(found).IsTrue();
        await Assert.That(pc.Alignment).IsEqualTo(175);
    }

    // ── WithCharacter / GetPendingMobileMdy ───────────────────────────────────

    [Test]
    public async Task WithCharacter_QueuesUpdate_PendingReflectsNewLevel()
    {
        var (save, mdyPath) = MakeSaveWithPc(level: 5);
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);

        var updated = pc.ToBuilder().WithLevel(15).Build();
        editor.WithCharacter(mdyPath, c => c.Level == 5, updated);

        var pending = editor.GetPendingMobileMdy(mdyPath);
        await Assert.That(pending).IsNotNull();
        var character = pending!.Characters.First();
        await Assert.That(character.Stats[17]).IsEqualTo(15); // stats[17] = level
    }

    [Test]
    public async Task WithCharacter_QueuesUpdate_PendingReflectsNewAlignment()
    {
        var (save, mdyPath) = MakeSaveWithPc(level: 5, alignment: 100);
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);

        var updated = pc.ToBuilder().WithAlignment(30).Build();
        editor.WithCharacter(mdyPath, c => c.HasCompleteData, updated);

        var pending = editor.GetPendingMobileMdy(mdyPath);
        await Assert.That(pending).IsNotNull();
        var character = pending!.Characters.First();
        await Assert.That(character.Stats[19]).IsEqualTo(30); // stats[19] = alignment
    }

    [Test]
    public async Task WithCharacter_ChainedCalls_ApplyAllUpdates()
    {
        var (save, mdyPath) = MakeSaveWithPc(level: 5, alignment: 100);
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);

        // First call: level 5 → 20.
        var updated1 = pc.ToBuilder().WithLevel(20).Build();
        editor.WithCharacter(mdyPath, c => c.Level == 5, updated1);

        // Second call: read from the pending state (not the original save) so we
        // don't overwrite the already-queued level=20 back to 5.
        var pendingBetween = editor.GetPendingMobileMdy(mdyPath)!;
        var pc2 = CharacterRecord.From(pendingBetween.Characters.First());
        var updated2 = pc2.ToBuilder().WithAlignment(10).Build();
        editor.WithCharacter(mdyPath, c => c.Level == 20, updated2);

        var pending = editor.GetPendingMobileMdy(mdyPath);
        var character = pending!.Characters.First();
        await Assert.That(character.Stats[17]).IsEqualTo(20); // level preserved
        await Assert.That(character.Stats[19]).IsEqualTo(10); // alignment updated
    }

    [Test]
    public async Task WithCharacter_UnknownPath_IsNoOp()
    {
        var (save, _) = MakeSaveWithPc();
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);

        var updated = pc.ToBuilder().WithLevel(99).Build();
        editor.WithCharacter("nonexistent/path/mobile.mdy", _ => true, updated);

        await Assert.That(editor.GetPendingMobileMdy("nonexistent/path/mobile.mdy")).IsNull();
    }

    [Test]
    public async Task GetPendingMobileMdy_ReturnsNull_BeforeAnyWithCharacterCall()
    {
        var (save, mdyPath) = MakeSaveWithPc();
        var editor = new SaveGameEditor(save);

        await Assert.That(editor.GetPendingMobileMdy(mdyPath)).IsNull();
    }

    // ── Save → Load round-trip ────────────────────────────────────────────────

    [Test]
    public async Task Save_WithLevelChange_RoundTrips()
    {
        var (save, mdyPath) = MakeSaveWithPc(level: 5);
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);
        editor.WithCharacter(mdyPath, c => c.Level == 5, pc.ToBuilder().WithLevel(25).Build());

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");
            new SaveGameEditor(loaded).TryFindPlayerCharacter(out var loadedPc, out _);

            await Assert.That(loadedPc.Level).IsEqualTo(25);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_WithAlignmentChange_RoundTrips()
    {
        var (save, mdyPath) = MakeSaveWithPc(level: 6, alignment: 100);
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);
        editor.WithCharacter(mdyPath, c => c.HasCompleteData, pc.ToBuilder().WithAlignment(175).Build());

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");
            new SaveGameEditor(loaded).TryFindPlayerCharacter(out var loadedPc, out _);

            await Assert.That(loadedPc.Alignment).IsEqualTo(175);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_WithGoldChange_RoundTrips()
    {
        var (save, mdyPath) = MakeSaveWithPc(level: 5, gold: 100);
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);
        editor.WithCharacter(mdyPath, c => c.HasCompleteData, pc.ToBuilder().WithGold(99999).Build());

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");
            new SaveGameEditor(loaded).TryFindPlayerCharacter(out var loadedPc, out _);

            await Assert.That(loadedPc.Gold).IsEqualTo(99999);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_WithNoUpdates_PreservesOriginalPc()
    {
        var (save, _) = MakeSaveWithPc(level: 8, alignment: 50, gold: 200);
        var editor = new SaveGameEditor(save);

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");
            new SaveGameEditor(loaded).TryFindPlayerCharacter(out var loadedPc, out _);

            await Assert.That(loadedPc.Level).IsEqualTo(8);
            await Assert.That(loadedPc.Alignment).IsEqualTo(50);
            await Assert.That(loadedPc.Gold).IsEqualTo(200);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_WithMultipleFieldChanges_RoundTrips()
    {
        var (save, mdyPath) = MakeSaveWithPc(level: 5, alignment: 100, gold: 50);
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);

        var updated = pc.ToBuilder().WithLevel(30).WithAlignment(20).WithGold(5000).WithStrength(18).Build();
        editor.WithCharacter(mdyPath, c => c.HasCompleteData, updated);

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");
            new SaveGameEditor(loaded).TryFindPlayerCharacter(out var loadedPc, out _);

            await Assert.That(loadedPc.Level).IsEqualTo(30);
            await Assert.That(loadedPc.Alignment).IsEqualTo(20);
            await Assert.That(loadedPc.Gold).IsEqualTo(5000);
            await Assert.That(loadedPc.Strength).IsEqualTo(18);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // ── CharacterRecord.From / ApplyTo bridge ──────────────────────────────────

    [Test]
    public async Task CharacterRecord_From_ReadsLevelAndAlignment()
    {
        var v2Bytes = BuildV2Record(level: 12, alignment: 80);
        var rec = CharacterMdyRecord.Parse(v2Bytes, out _);

        var cr = CharacterRecord.From(rec);

        await Assert.That(cr.Level).IsEqualTo(12);
        await Assert.That(cr.Alignment).IsEqualTo(80);
        await Assert.That(cr.HasCompleteData).IsTrue();
    }

    [Test]
    public async Task CharacterRecord_From_ReadsGold()
    {
        var v2Bytes = BuildV2Record(level: 5, gold: 750);
        var rec = CharacterMdyRecord.Parse(v2Bytes, out _);

        var cr = CharacterRecord.From(rec);

        await Assert.That(cr.Gold).IsEqualTo(750);
    }

    [Test]
    public async Task CharacterRecord_ApplyTo_PatchesLevel()
    {
        var v2Bytes = BuildV2Record(level: 5);
        var rec = CharacterMdyRecord.Parse(v2Bytes, out _);
        var cr = CharacterRecord.From(rec);

        var patched = cr.ToBuilder().WithLevel(20).Build().ApplyTo(rec);
        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);

        await Assert.That(reparsed.Stats[17]).IsEqualTo(20);
    }

    [Test]
    public async Task CharacterRecord_ApplyTo_PatchesAlignment()
    {
        var v2Bytes = BuildV2Record(alignment: 100);
        var rec = CharacterMdyRecord.Parse(v2Bytes, out _);
        var cr = CharacterRecord.From(rec);

        var patched = cr.ToBuilder().WithAlignment(50).Build().ApplyTo(rec);
        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);

        await Assert.That(reparsed.Stats[19]).IsEqualTo(50);
    }

    [Test]
    public async Task CharacterRecord_ApplyTo_PatchesGold()
    {
        var v2Bytes = BuildV2Record(gold: 500);
        var rec = CharacterMdyRecord.Parse(v2Bytes, out _);
        var cr = CharacterRecord.From(rec);

        var patched = cr.ToBuilder().WithGold(9999).Build().ApplyTo(rec);
        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);

        await Assert.That(reparsed.Gold).IsEqualTo(9999);
    }

    [Test]
    public async Task CharacterRecord_ApplyTo_PreservesUnchangedStats()
    {
        var v2Bytes = BuildV2Record(level: 5, alignment: 100, gold: 500);
        var rec = CharacterMdyRecord.Parse(v2Bytes, out _);
        var cr = CharacterRecord.From(rec);

        // Change only level; gold and alignment should be preserved.
        var patched = cr.ToBuilder().WithLevel(10).Build().ApplyTo(rec);
        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);

        await Assert.That(reparsed.Stats[17]).IsEqualTo(10); // level updated
        await Assert.That(reparsed.Stats[19]).IsEqualTo(100); // alignment preserved
        await Assert.That(reparsed.Gold).IsEqualTo(500); // gold preserved
    }

    [Test]
    public async Task CharacterRecord_ToBuilder_ProducesIdenticalRecord()
    {
        var v2Bytes = BuildV2Record(level: 7, alignment: 55, gold: 200);
        var rec = CharacterMdyRecord.Parse(v2Bytes, out _);
        var cr = CharacterRecord.From(rec);

        var rebuilt = cr.ToBuilder().Build();

        await Assert.That(rebuilt.Level).IsEqualTo(cr.Level);
        await Assert.That(rebuilt.Alignment).IsEqualTo(cr.Alignment);
        await Assert.That(rebuilt.Gold).IsEqualTo(cr.Gold);
        await Assert.That(rebuilt.HasCompleteData).IsEqualTo(cr.HasCompleteData);
    }

    // ── CharacterRecord — name field ──────────────────────────────────────────

    /// <summary>
    /// Builds a v2 record that includes a name field, gold SAR, HP SAR (bsId=0x4046),
    /// fatigue SAR (bsId=0x423E), portrait SAR (bsId=0x4DA4), and game-stats SAR
    /// (bsId=0x4D68) after the four primary SARs so all optional fields can be tested.
    /// </summary>
    private static byte[] BuildRichV2Record(
        int level = 5,
        int gold = 0,
        int hpDamage = 0,
        int fatigueDamage = 0,
        int portraitIndex = -1,
        string? name = null,
        int bowSkill = 0,
        int necroBlackCollege = 0
    )
    {
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        var statsData = new byte[28 * 4];
        BinaryPrimitives.WriteInt32LittleEndian(statsData.AsSpan(17 * 4, 4), level);
        var statsSar = Sar(4, 28, 0x4DA5, statsData);

        // basic skills: index 0 = Bow
        var basicData = new byte[12 * 4];
        BinaryPrimitives.WriteInt32LittleEndian(basicData.AsSpan(0 * 4, 4), bowSkill);
        var basicSar = Sar(4, 12, 0x43C3, basicData);

        var techSar = Sar(4, 4, 0x4A07, IntArray(4));

        // spell/tech: index 11 = NecroBlack
        var spellData = new byte[25 * 4];
        BinaryPrimitives.WriteInt32LittleEndian(spellData.AsSpan(11 * 4, 4), necroBlackCollege);
        var spellSar = Sar(4, 25, 0x4A08, spellData);

        // Gold SAR
        var goldData = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(goldData, gold);
        var goldSar = Sar(4, 1, 0x4B13, goldData);

        // HP SAR (bsId=0x4046): [AcBonus, HpPtsBonus, HpAdj, HpDamage]
        var hpData = new byte[4 * 4];
        BinaryPrimitives.WriteInt32LittleEndian(hpData.AsSpan(3 * 4, 4), hpDamage);
        var hpSar = Sar(4, 4, 0x4046, hpData);

        // Fatigue SAR (bsId=0x423E): [FatiguePtsBonus, FatigueAdj, FatigueDamage, ?]
        var fatData = new byte[4 * 4];
        BinaryPrimitives.WriteInt32LittleEndian(fatData.AsSpan(2 * 4, 4), fatigueDamage);
        var fatSar = Sar(4, 4, 0x423E, fatData);

        // Portrait SAR (bsId=0x4DA4): [MaxFollowers, PortraitIndex, 0]
        var portData = new byte[3 * 4];
        BinaryPrimitives.WriteInt32LittleEndian(portData.AsSpan(1 * 4, 4), Math.Max(0, portraitIndex));
        var portSar = Sar(4, 3, 0x4DA4, portData);

        var parts = new List<byte[]> { magic, hpSar, fatSar, statsSar, basicSar, techSar, spellSar, goldSar, portSar };

        // Name field: 0x01 [uint32 len] [ascii bytes]
        if (name is not null)
        {
            var nameBytes = System.Text.Encoding.ASCII.GetBytes(name);
            var nameField = new byte[1 + 4 + nameBytes.Length];
            nameField[0] = 0x01;
            BinaryPrimitives.WriteInt32LittleEndian(nameField.AsSpan(1, 4), nameBytes.Length);
            nameBytes.CopyTo(nameField, 5);
            parts.Add(nameField);
        }

        return [.. parts.SelectMany(b => b)];
    }

    private static (LoadedSave save, string mdyPath) MakeSaveWithRichPc(
        int level = 5,
        int gold = 0,
        int hpDamage = 0,
        int fatigueDamage = 0,
        int portraitIndex = -1,
        string? name = null,
        int bowSkill = 0,
        int necroBlackCollege = 0
    )
    {
        var v2Bytes = BuildRichV2Record(
            level,
            gold,
            hpDamage,
            fatigueDamage,
            portraitIndex,
            name,
            bowSkill,
            necroBlackCollege
        );
        const string mdyPath = "maps/Arcanum1-024/mobile.mdy";
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase) { [mdyPath] = v2Bytes };
        var index = new SaveIndex
        {
            Root =
            [
                new TfaiDirectoryEntry
                {
                    Name = "maps",
                    Children =
                    [
                        new TfaiDirectoryEntry
                        {
                            Name = "Arcanum1-024",
                            Children = [new TfaiFileEntry { Name = "mobile.mdy", Size = v2Bytes.Length }],
                        },
                    ],
                },
            ],
        };
        var info = new SaveInfo
        {
            ModuleName = "arcanum",
            LeaderName = name ?? "TestPC",
            DisplayName = "Rich Test Save",
            MapId = 24,
            GameTimeDays = 0,
            GameTimeMs = 0,
            LeaderPortraitId = Math.Max(0, portraitIndex),
            LeaderLevel = Math.Max(1, level),
            LeaderTileX = 0,
            LeaderTileY = 0,
            StoryState = 0,
        };
        var tfafBytes = TfafFormat.Pack(index, files);
        return (SaveGameLoader.LoadFromParsed(info, index, tfafBytes), mdyPath);
    }

    [Test]
    public async Task CharacterRecord_From_ReadsHpDamage()
    {
        var bytes = BuildRichV2Record(hpDamage: 25);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(CharacterRecord.From(rec).HpDamage).IsEqualTo(25);
    }

    [Test]
    public async Task CharacterRecord_From_ReadsFatigueDamage()
    {
        var bytes = BuildRichV2Record(fatigueDamage: 15);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(CharacterRecord.From(rec).FatigueDamage).IsEqualTo(15);
    }

    [Test]
    public async Task CharacterRecord_From_ReadsName()
    {
        var bytes = BuildRichV2Record(name: "Roberta");
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(CharacterRecord.From(rec).Name).IsEqualTo("Roberta");
    }

    [Test]
    public async Task CharacterRecord_From_ReadsPortraitIndex()
    {
        var bytes = BuildRichV2Record(portraitIndex: 7);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(CharacterRecord.From(rec).PortraitIndex).IsEqualTo(7);
    }

    [Test]
    public async Task CharacterRecord_From_ReadsBowSkill()
    {
        var bytes = BuildRichV2Record(bowSkill: 5);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(CharacterRecord.From(rec).SkillBow).IsEqualTo(5);
    }

    [Test]
    public async Task CharacterRecord_From_ReadsNecroBlackCollege()
    {
        var bytes = BuildRichV2Record(necroBlackCollege: 3);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(CharacterRecord.From(rec).SpellNecroBlack).IsEqualTo(3);
    }

    [Test]
    public async Task Save_WithNameChange_RoundTrips()
    {
        var (save, mdyPath) = MakeSaveWithRichPc(name: "OldName");
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);
        editor.WithCharacter(mdyPath, c => c.HasCompleteData, pc.ToBuilder().WithName("NewName").Build());

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");
            new SaveGameEditor(loaded).TryFindPlayerCharacter(out var loadedPc, out _);
            await Assert.That(loadedPc.Name).IsEqualTo("NewName");
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_WithHpDamageEdit_RoundTrips()
    {
        var (save, mdyPath) = MakeSaveWithRichPc(hpDamage: 0);
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);
        editor.WithCharacter(mdyPath, c => c.HasCompleteData, pc.ToBuilder().WithHpDamageRaw([0, 0, 0, 30]).Build());

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");
            new SaveGameEditor(loaded).TryFindPlayerCharacter(out var loadedPc, out _);
            await Assert.That(loadedPc.HpDamage).IsEqualTo(30);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_WithFatigueDamageEdit_RoundTrips()
    {
        var (save, mdyPath) = MakeSaveWithRichPc(fatigueDamage: 0);
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);
        editor.WithCharacter(
            mdyPath,
            c => c.HasCompleteData,
            pc.ToBuilder().WithFatigueDamageRaw([0, 0, 20, 0]).Build()
        );

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");
            new SaveGameEditor(loaded).TryFindPlayerCharacter(out var loadedPc, out _);
            await Assert.That(loadedPc.FatigueDamage).IsEqualTo(20);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_WithBasicSkillsChange_RoundTrips()
    {
        var (save, mdyPath) = MakeSaveWithRichPc(bowSkill: 0);
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);
        editor.WithCharacter(mdyPath, c => c.HasCompleteData, pc.ToBuilder().WithSkillBow(5).WithSkillMelee(3).Build());

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");
            new SaveGameEditor(loaded).TryFindPlayerCharacter(out var loadedPc, out _);
            await Assert.That(loadedPc.SkillBow).IsEqualTo(5);
            await Assert.That(loadedPc.SkillMelee).IsEqualTo(3);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_WithSpellCollegeChange_RoundTrips()
    {
        var (save, mdyPath) = MakeSaveWithRichPc(necroBlackCollege: 0);
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);
        editor.WithCharacter(
            mdyPath,
            c => c.HasCompleteData,
            pc.ToBuilder().WithSpellNecroBlack(4).WithSpellFire(2).Build()
        );

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");
            new SaveGameEditor(loaded).TryFindPlayerCharacter(out var loadedPc, out _);
            await Assert.That(loadedPc.SpellNecroBlack).IsEqualTo(4);
            await Assert.That(loadedPc.SpellFire).IsEqualTo(2);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_WithTechDisciplineChange_RoundTrips()
    {
        var (save, mdyPath) = MakeSaveWithRichPc();
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);
        editor.WithCharacter(
            mdyPath,
            c => c.HasCompleteData,
            pc.ToBuilder().WithTechGun(3).WithTechElectric(2).Build()
        );

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");
            new SaveGameEditor(loaded).TryFindPlayerCharacter(out var loadedPc, out _);
            await Assert.That(loadedPc.TechGun).IsEqualTo(3);
            await Assert.That(loadedPc.TechElectric).IsEqualTo(2);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_FullCharacterEdit_AllFieldsRoundTrip()
    {
        var (save, mdyPath) = MakeSaveWithRichPc(
            level: 1,
            gold: 0,
            hpDamage: 0,
            fatigueDamage: 0,
            portraitIndex: 1,
            name: "Original",
            bowSkill: 0,
            necroBlackCollege: 0
        );
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);

        var updated = pc.ToBuilder()
            .WithLevel(15)
            .WithAlignment(25)
            .WithGold(12345)
            .WithName("Edited")
            .WithStrength(16)
            .WithIntelligence(18)
            .WithSkillBow(4)
            .WithSkillPersuasion(3)
            .WithSpellNecroBlack(5)
            .WithSpellTemporal(2)
            .WithTechGun(1)
            .WithHpDamageRaw([0, 0, 0, 10])
            .WithFatigueDamageRaw([0, 0, 5, 0])
            .Build();
        editor.WithCharacter(mdyPath, c => c.HasCompleteData, updated);

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");
            new SaveGameEditor(loaded).TryFindPlayerCharacter(out var r, out _);

            await Assert.That(r.Level).IsEqualTo(15);
            await Assert.That(r.Alignment).IsEqualTo(25);
            await Assert.That(r.Gold).IsEqualTo(12345);
            await Assert.That(r.Name).IsEqualTo("Edited");
            await Assert.That(r.Strength).IsEqualTo(16);
            await Assert.That(r.Intelligence).IsEqualTo(18);
            await Assert.That(r.SkillBow).IsEqualTo(4);
            await Assert.That(r.SkillPersuasion).IsEqualTo(3);
            await Assert.That(r.SpellNecroBlack).IsEqualTo(5);
            await Assert.That(r.SpellTemporal).IsEqualTo(2);
            await Assert.That(r.TechGun).IsEqualTo(1);
            await Assert.That(r.HpDamage).IsEqualTo(10);
            await Assert.That(r.FatigueDamage).IsEqualTo(5);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }
}
