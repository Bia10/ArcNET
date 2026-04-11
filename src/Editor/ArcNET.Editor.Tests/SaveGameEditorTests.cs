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

    // Builds a SAR with an explicit bsCnt + bitset words (needed for quest/rep/blessing/etc. detection).
    private static byte[] SarWithBitset(int elemSz, int elemCnt, int bsId, byte[] data, int bsCnt, int[]? bitset = null)
    {
        var bitsetBytes = bitset is null ? new byte[bsCnt * 4] : new byte[bsCnt * 4];
        if (bitset is not null)
            for (var i = 0; i < Math.Min(bitset.Length, bsCnt); i++)
                BinaryPrimitives.WriteInt32LittleEndian(bitsetBytes.AsSpan(i * 4, 4), bitset[i]);

        var buf = new byte[13 + data.Length + 4 + bitsetBytes.Length];
        buf[0] = 0x01;
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(1, 4), elemSz);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(5, 4), elemCnt);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(9, 4), bsId);
        data.CopyTo(buf, 13);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(13 + data.Length, 4), bsCnt);
        bitsetBytes.CopyTo(buf, 13 + data.Length + 4);
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

    private static (LoadedSave save, string mdyPath) MakeSaveWithPcRecords(params byte[][] records)
    {
        var mdyBytes = records.SelectMany(static b => b).ToArray();
        const string mdyPath = "maps/Arcanum1-024/mobile.mdy";

        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase) { [mdyPath] = mdyBytes };

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
                            Children = [new TfaiFileEntry { Name = "mobile.mdy", Size = mdyBytes.Length }],
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
            LeaderLevel = 1,
            LeaderTileX = 0,
            LeaderTileY = 0,
            StoryState = 0,
        };

        var tfafBytes = TfafFormat.Pack(index, files);
        return (SaveGameLoader.LoadFromParsed(info, index, tfafBytes), mdyPath);
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
    public async Task WithCharacter_OnlyReplacesFirstMatchingRecord()
    {
        var (save, mdyPath) = MakeSaveWithPcRecords(BuildV2Record(level: 5), BuildV2Record(level: 5));
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);

        editor.WithCharacter(mdyPath, c => c.Level == 5, pc.ToBuilder().WithLevel(20).Build());

        var pending = editor.GetPendingMobileMdy(mdyPath);
        var pendingCharacters = pending!.Characters.ToList();
        await Assert.That(pending).IsNotNull();
        await Assert.That(pendingCharacters.Count).IsEqualTo(2);
        await Assert.That(pendingCharacters[0].Stats[17]).IsEqualTo(20);
        await Assert.That(pendingCharacters[1].Stats[17]).IsEqualTo(5);
    }

    [Test]
    public async Task WithPlayerCharacter_UsesPendingStateAcrossChainedCalls()
    {
        var (save, _) = MakeSaveWithPc(level: 5, alignment: 100);
        var editor = new SaveGameEditor(save);

        editor
            .WithPlayerCharacter(pc => pc.ToBuilder().WithLevel(20).Build())
            .WithPlayerCharacter(pc => pc.ToBuilder().WithAlignment(10).Build());

        var found = editor.TryFindPendingPlayerCharacter(out var pendingPlayer);

        await Assert.That(found).IsTrue();
        await Assert.That(pendingPlayer.Level).IsEqualTo(20);
        await Assert.That(pendingPlayer.Alignment).IsEqualTo(10);
    }

    [Test]
    public async Task WithSaveInfo_UsesPendingStateAcrossChainedCalls()
    {
        var (save, _) = MakeSaveWithPc();
        var editor = new SaveGameEditor(save);

        editor
            .WithSaveInfo(info => info.With(displayName: "Renamed Save"))
            .WithSaveInfo(info => info.With(gameTimeDays: 4, gameTimeMs: 1234, storyState: 7));

        var currentInfo = editor.GetCurrentSaveInfo();
        var pendingInfo = editor.GetPendingSaveInfo();

        await Assert.That(pendingInfo).IsNotNull();
        await Assert.That(currentInfo.DisplayName).IsEqualTo("Renamed Save");
        await Assert.That(currentInfo.GameTimeDays).IsEqualTo(4);
        await Assert.That(currentInfo.GameTimeMs).IsEqualTo(1234);
        await Assert.That(currentInfo.StoryState).IsEqualTo(7);
    }

    [Test]
    public async Task WithSaveInfo_ComposesWithPlayerSync_ForLeaderFields()
    {
        var (save, _) = MakeSaveWithRichPc(level: 5, portraitIndex: 1, name: "OldName");
        var editor = new SaveGameEditor(save);

        editor
            .WithSaveInfo(info =>
            {
                return info.With(
                    displayName: "Renamed Save",
                    storyState: 9,
                    leaderName: "ManualName",
                    leaderLevel: 99,
                    leaderPortraitId: 42
                );
            })
            .WithPlayerCharacter(pc => pc.ToBuilder().WithLevel(12).WithPortraitIndex(7).WithName("NewName").Build());

        var pendingInfo = editor.GetPendingSaveInfo();

        await Assert.That(pendingInfo).IsNotNull();
        await Assert.That(pendingInfo!.DisplayName).IsEqualTo("Renamed Save");
        await Assert.That(pendingInfo.StoryState).IsEqualTo(9);
        await Assert.That(pendingInfo.LeaderName).IsEqualTo("NewName");
        await Assert.That(pendingInfo.LeaderLevel).IsEqualTo(12);
        await Assert.That(pendingInfo.LeaderPortraitId).IsEqualTo(7);
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
    public async Task Save_WithPlayerCharacterUpdate_SyncsLeaderMetadata()
    {
        var (save, _) = MakeSaveWithRichPc(level: 5, portraitIndex: 1, name: "OldName");
        var editor = new SaveGameEditor(save);

        editor.WithPlayerCharacter(pc => pc.ToBuilder().WithLevel(12).WithPortraitIndex(7).WithName("NewName").Build());

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");

            await Assert.That(loaded.Info.LeaderName).IsEqualTo("NewName");
            await Assert.That(loaded.Info.LeaderLevel).IsEqualTo(12);
            await Assert.That(loaded.Info.LeaderPortraitId).IsEqualTo(7);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_WithSaveInfoUpdate_RoundTrips()
    {
        var (save, _) = MakeSaveWithPc();
        var editor = new SaveGameEditor(save);

        editor.WithSaveInfo(info =>
        {
            return info.With(
                displayName: "Manual Save Name",
                mapId: 77,
                gameTimeDays: 5,
                gameTimeMs: 6789,
                leaderTileX: 12,
                leaderTileY: 34,
                storyState: 3
            );
        });

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");

            await Assert.That(loaded.Info.DisplayName).IsEqualTo("Manual Save Name");
            await Assert.That(loaded.Info.MapId).IsEqualTo(77);
            await Assert.That(loaded.Info.GameTimeDays).IsEqualTo(5);
            await Assert.That(loaded.Info.GameTimeMs).IsEqualTo(6789);
            await Assert.That(loaded.Info.LeaderTileX).IsEqualTo(12);
            await Assert.That(loaded.Info.LeaderTileY).IsEqualTo(34);
            await Assert.That(loaded.Info.StoryState).IsEqualTo(3);
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

    // ── Builder convenience — scalar HP / Fatigue setters ────────────────────

    [Test]
    public async Task Builder_WithHpDamage_SetsElement3_PreservesOthers()
    {
        // Source record has HpDamageRaw [10, 20, 30, 0]
        var bytes = BuildRichV2Record();
        var src = CharacterMdyRecord.Parse(bytes, out _);
        // Patch elements 0-2 to known non-zero values so we can verify they survive
        var patched = src.WithHpDamage([10, 20, 30, 0]);
        var cr = CharacterRecord.From(patched);

        var built = cr.ToBuilder().WithHpDamage(99).Build();

        await Assert.That(built.HpDamageRaw![0]).IsEqualTo(10);
        await Assert.That(built.HpDamageRaw![1]).IsEqualTo(20);
        await Assert.That(built.HpDamageRaw![2]).IsEqualTo(30);
        await Assert.That(built.HpDamageRaw![3]).IsEqualTo(99);
        await Assert.That(built.HpDamage).IsEqualTo(99);
    }

    [Test]
    public async Task Builder_WithHpDamage_WhenRawIsNull_CreatesFreshArray()
    {
        var cr = new CharacterRecord.Builder().WithHpDamage(42).Build();

        await Assert.That(cr.HpDamageRaw).IsNotNull();
        await Assert.That(cr.HpDamage).IsEqualTo(42);
        await Assert.That(cr.HpDamageRaw![0]).IsEqualTo(0);
    }

    [Test]
    public async Task Builder_WithFatigueDamage_SetsElement2_PreservesOthers()
    {
        var bytes = BuildRichV2Record();
        var src = CharacterMdyRecord.Parse(bytes, out _);
        var patched = src.WithFatigueDamage([5, 15, 0, 25]);
        var cr = CharacterRecord.From(patched);

        var built = cr.ToBuilder().WithFatigueDamage(7).Build();

        await Assert.That(built.FatigueDamageRaw![0]).IsEqualTo(5);
        await Assert.That(built.FatigueDamageRaw![1]).IsEqualTo(15);
        await Assert.That(built.FatigueDamageRaw![2]).IsEqualTo(7);
        await Assert.That(built.FatigueDamageRaw![3]).IsEqualTo(25);
        await Assert.That(built.FatigueDamage).IsEqualTo(7);
    }

    // ── Builder — derived stat With* methods ─────────────────────────────────

    [Test]
    public async Task Builder_WithMaxFollowers_RoundTrips()
    {
        var (save, mdyPath) = MakeSaveWithRichPc();
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);
        editor.WithCharacter(mdyPath, c => c.HasCompleteData, pc.ToBuilder().WithMaxFollowers(6).Build());

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");
            new SaveGameEditor(loaded).TryFindPlayerCharacter(out var r, out _);
            await Assert.That(r.MaxFollowers).IsEqualTo(6);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Builder_WithMagickTechAptitude_RoundTrips()
    {
        var (save, mdyPath) = MakeSaveWithRichPc();
        var editor = new SaveGameEditor(save);
        editor.TryFindPlayerCharacter(out var pc, out _);
        editor.WithCharacter(mdyPath, c => c.HasCompleteData, pc.ToBuilder().WithMagickTechAptitude(-80).Build());

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            editor.Save(tmpDir, "test");
            var loaded = SaveGameLoader.Load(tmpDir, "test");
            new SaveGameEditor(loaded).TryFindPlayerCharacter(out var r, out _);
            await Assert.That(r.MagickTechAptitude).IsEqualTo(-80);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // ── Builder — Bullets / PowerCells (magic char: no-op on GameStats SAR) ─

    [Test]
    public async Task CharacterRecord_Bullets_DefaultsToZero_OnMagicChar()
    {
        var bytes = BuildRichV2Record();
        var rec = CharacterMdyRecord.Parse(bytes, out _);
        await Assert.That(CharacterRecord.From(rec).Bullets).IsEqualTo(0);
    }

    [Test]
    public async Task CharacterRecord_PowerCells_DefaultsToZero_OnMagicChar()
    {
        var bytes = BuildRichV2Record();
        var rec = CharacterMdyRecord.Parse(bytes, out _);
        await Assert.That(CharacterRecord.From(rec).PowerCells).IsEqualTo(0);
    }

    [Test]
    public async Task Builder_WithBullets_SetsField_RetainedInRecord()
    {
        var cr = new CharacterRecord.Builder().WithBullets(50).Build();
        await Assert.That(cr.Bullets).IsEqualTo(50);
    }

    [Test]
    public async Task Builder_WithPowerCells_SetsField_RetainedInRecord()
    {
        var cr = new CharacterRecord.Builder().WithPowerCells(12).Build();
        await Assert.That(cr.PowerCells).IsEqualTo(12);
    }

    // ── CharacterRecord raw-field bridge (quest / rep / blessing / curse / schematics / rumors) ──

    // Builds a v2 record containing quest, reputation, blessing, curse, schematics, and rumors SARs.
    private static byte[] BuildV2RecordWithAdvancedFields()
    {
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        var statsData = new byte[28 * 4];
        BinaryPrimitives.WriteInt32LittleEndian(statsData.AsSpan(17 * 4, 4), 10); // level=10
        var statsSar = Sar(4, 28, 0x4DA5, statsData);
        var basicSar = Sar(4, 12, 0x43C3, IntArray(12));
        var techSar = Sar(4, 4, 0x4A07, IntArray(4));
        var spellSar = Sar(4, 25, 0x4A08, IntArray(25));

        // Quest SAR: eSize=16, bsCnt=37, eCnt=2 — fingerprint requires bsCnt==37
        var questEntries = new byte[2 * 16]; // two 16-byte entries (all zeros)
        // Set quest proto IDs in bitset: bits 1 and 5 (quest slots 1 and 5)
        var questBitset = new int[37];
        questBitset[0] = (1 << 1) | (1 << 5);
        var questSar = SarWithBitset(16, 2, 0x4A00, questEntries, 37, questBitset);

        // Reputation SAR: eSize=4, eCnt=19, bsCnt=3 — exact fingerprint required
        var repData = new byte[19 * 4];
        BinaryPrimitives.WriteInt32LittleEndian(repData.AsSpan(0, 4), 1234); // slot 0 value
        var repBitset = new int[3];
        repBitset[0] = 0x1FFF; // slots 0-12
        repBitset[2] = 0x003F << 0; // slots 64-69 (bits 0-5 of word 2)
        var repSar = SarWithBitset(4, 19, 0x48AA, repData, 3, repBitset);

        // Blessing SARs: first 4:2:2 + consecutive 8:2:2 pair
        var blessProtoData = new byte[2 * 4];
        BinaryPrimitives.WriteInt32LittleEndian(blessProtoData.AsSpan(0, 4), 1049); // god 1
        BinaryPrimitives.WriteInt32LittleEndian(blessProtoData.AsSpan(4, 4), 1051); // god 2
        var blessProtoSar = SarWithBitset(4, 2, 0x48E9, blessProtoData, 2);
        var blessTsData = new byte[2 * 8]; // 8 bytes per timestamp, 2 entries
        var blessTsSar = SarWithBitset(8, 2, 0x48EA, blessTsData, 2);

        // Schematics SAR: standalone 4:3:2 with first value > 1000
        var schData = new byte[3 * 4];
        BinaryPrimitives.WriteInt32LittleEndian(schData.AsSpan(0, 4), 5090);
        BinaryPrimitives.WriteInt32LittleEndian(schData.AsSpan(4, 4), 4810);
        BinaryPrimitives.WriteInt32LittleEndian(schData.AsSpan(8, 4), 4010);
        var schSar = SarWithBitset(4, 3, 0x5228, schData, 2);

        // Rumors SAR: eSize=8, bsCnt=39, eCnt=3
        var rumorsData = new byte[3 * 8];
        var rumorsSar = SarWithBitset(8, 3, 0x4D89, rumorsData, 39);

        return
        [
            .. magic,
            .. statsSar,
            .. basicSar,
            .. techSar,
            .. spellSar,
            .. questSar,
            .. repSar,
            .. blessProtoSar,
            .. blessTsSar,
            .. schSar,
            .. rumorsSar,
        ];
    }

    [Test]
    public async Task CharacterRecord_From_ExposesQuestCount()
    {
        var v2Bytes = BuildV2RecordWithAdvancedFields();
        var mdyRecord = CharacterMdyRecord.Parse(v2Bytes, out _);
        var cr = CharacterRecord.From(mdyRecord);

        await Assert.That(cr.QuestCount).IsEqualTo(2);
        await Assert.That(cr.QuestDataRaw).IsNotNull();
        await Assert.That(cr.QuestDataRaw!.Length).IsEqualTo(2 * 16);
        await Assert.That(cr.QuestBitsetRaw).IsNotNull();
        await Assert.That(cr.QuestBitsetRaw![0]).IsEqualTo((1 << 1) | (1 << 5));
    }

    [Test]
    public async Task CharacterRecord_From_ExposesReputation()
    {
        var v2Bytes = BuildV2RecordWithAdvancedFields();
        var mdyRecord = CharacterMdyRecord.Parse(v2Bytes, out _);
        var cr = CharacterRecord.From(mdyRecord);

        await Assert.That(cr.ReputationRaw).IsNotNull();
        await Assert.That(cr.ReputationRaw!.Length).IsEqualTo(19);
        await Assert.That(cr.ReputationRaw[0]).IsEqualTo(1234);
    }

    [Test]
    public async Task CharacterRecord_From_ExposesBlessings()
    {
        var v2Bytes = BuildV2RecordWithAdvancedFields();
        var mdyRecord = CharacterMdyRecord.Parse(v2Bytes, out _);
        var cr = CharacterRecord.From(mdyRecord);

        await Assert.That(cr.BlessingProtoElementCount).IsEqualTo(2);
        await Assert.That(cr.BlessingRaw).IsNotNull();
        await Assert.That(cr.BlessingRaw![0]).IsEqualTo(1049);
        await Assert.That(cr.BlessingRaw[1]).IsEqualTo(1051);
        await Assert.That(cr.BlessingTsRaw).IsNotNull();
        await Assert.That(cr.BlessingTsRaw!.Length).IsEqualTo(2 * 8);
    }

    [Test]
    public async Task CharacterRecord_From_ExposesSchematicsAndRumors()
    {
        var v2Bytes = BuildV2RecordWithAdvancedFields();
        var mdyRecord = CharacterMdyRecord.Parse(v2Bytes, out _);
        var cr = CharacterRecord.From(mdyRecord);

        await Assert.That(cr.SchematicsElementCount).IsEqualTo(3);
        await Assert.That(cr.SchematicsRaw).IsNotNull();
        await Assert.That(cr.SchematicsRaw![0]).IsEqualTo(5090);
        await Assert.That(cr.RumorsCount).IsEqualTo(3);
        await Assert.That(cr.RumorsRaw).IsNotNull();
        await Assert.That(cr.RumorsRaw!.Length).IsEqualTo(3 * 8);
    }

    [Test]
    public async Task Builder_WithReputationRaw_RoundTrips()
    {
        var rep = new int[19];
        rep[0] = 7777;
        rep[5] = -500;
        var cr = new CharacterRecord.Builder().WithReputationRaw(rep).Build();

        await Assert.That(cr.ReputationRaw).IsNotNull();
        await Assert.That(cr.ReputationRaw![0]).IsEqualTo(7777);
        await Assert.That(cr.ReputationRaw[5]).IsEqualTo(-500);
    }

    [Test]
    public async Task Builder_WithQuestDataRaw_RoundTrips()
    {
        var data = new byte[3 * 16];
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(8, 4), 2); // entry[0] state=2 (completed)
        var bitset = new int[37];
        bitset[0] = 0x15; // quest slots 0,2,4

        var cr = new CharacterRecord.Builder().WithQuestDataRaw(data).WithQuestBitsetRaw(bitset).Build();

        await Assert.That(cr.QuestCount).IsEqualTo(3);
        await Assert.That(cr.QuestDataRaw).IsNotNull();
        await Assert.That(cr.QuestBitsetRaw).IsNotNull();
        await Assert.That(cr.QuestBitsetRaw![0]).IsEqualTo(0x15);
    }

    [Test]
    public async Task Builder_WithSchematicsRaw_RoundTrips()
    {
        var sch = new int[] { 4010, 4810, 5090 };
        var cr = new CharacterRecord.Builder().WithSchematicsRaw(sch).Build();

        await Assert.That(cr.SchematicsElementCount).IsEqualTo(3);
        await Assert.That(cr.SchematicsRaw).IsNotNull();
        await Assert.That(cr.SchematicsRaw![1]).IsEqualTo(4810);
    }

    [Test]
    public async Task Builder_WithRumorsRaw_RoundTrips()
    {
        var rumors = new byte[5 * 8];
        var cr = new CharacterRecord.Builder().WithRumorsRaw(rumors).Build();

        await Assert.That(cr.RumorsCount).IsEqualTo(5);
        await Assert.That(cr.RumorsRaw).IsNotNull();
        await Assert.That(cr.RumorsRaw!.Length).IsEqualTo(40);
    }

    [Test]
    public async Task ApplyTo_WithReputation_PreservesReputationInRoundTrip()
    {
        var v2Bytes = BuildV2RecordWithAdvancedFields();
        var original = CharacterMdyRecord.Parse(v2Bytes, out _);
        var cr = CharacterRecord.From(original);

        // Patch reputation slot 0 through the Builder
        var newRep = (int[])cr.ReputationRaw!.Clone();
        newRep[0] = 9999;
        var updated = cr.ToBuilder().WithReputationRaw(newRep).Build();

        var patched = updated.ApplyTo(original);
        var roundTripped = CharacterRecord.From(patched);

        await Assert.That(roundTripped.ReputationRaw).IsNotNull();
        await Assert.That(roundTripped.ReputationRaw![0]).IsEqualTo(9999);
    }
}
