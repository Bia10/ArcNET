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
        // Use a valid empty JMP file so that no parse errors appear in a "valid" save.
        var nonMobBytes = JmpFormat.WriteToArray(new JmpFile { Jumps = [] });

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

    // ── Dialog loading ────────────────────────────────────────────────────────

    [Test]
    public async Task LoadFromParsed_ParsesDlgFiles()
    {
        var dlg = new DlgFile
        {
            Entries =
            [
                new DialogEntry
                {
                    Num = 1,
                    Text = "Hello traveller.",
                    GenderField = string.Empty,
                    Iq = 0,
                    Conditions = string.Empty,
                    ResponseVal = 0,
                    Actions = string.Empty,
                },
            ],
        };
        var dlgBytes = DialogFormat.WriteToArray(dlg);

        var mobBytes = MobFormat.WriteToArray(MakePc());
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = mobBytes,
            ["modules/arcanum/npc/guard.dlg"] = dlgBytes,
        };

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
                new TfaiDirectoryEntry
                {
                    Name = "modules",
                    Children =
                    [
                        new TfaiDirectoryEntry
                        {
                            Name = "arcanum",
                            Children =
                            [
                                new TfaiDirectoryEntry
                                {
                                    Name = "npc",
                                    Children = [new TfaiFileEntry { Name = "guard.dlg", Size = dlgBytes.Length }],
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

        await Assert.That(save.Dialogs.ContainsKey("modules/arcanum/npc/guard.dlg")).IsTrue();
        var loaded = save.Dialogs["modules/arcanum/npc/guard.dlg"];
        await Assert.That(loaded.Entries.Count).IsEqualTo(1);
        await Assert.That(loaded.Entries[0].Text).IsEqualTo("Hello traveller.");
    }

    [Test]
    public async Task Save_WithUpdatedDialog_PersistsChanges()
    {
        var dlg = new DlgFile
        {
            Entries =
            [
                new DialogEntry
                {
                    Num = 1,
                    Text = "Original text.",
                    GenderField = string.Empty,
                    Iq = 0,
                    Conditions = string.Empty,
                    ResponseVal = 0,
                    Actions = string.Empty,
                },
            ],
        };
        var dlgBytes = DialogFormat.WriteToArray(dlg);

        var mobBytes = MobFormat.WriteToArray(MakePc());
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = mobBytes,
            ["npc/guard.dlg"] = dlgBytes,
        };

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
                new TfaiDirectoryEntry
                {
                    Name = "npc",
                    Children = [new TfaiFileEntry { Name = "guard.dlg", Size = dlgBytes.Length }],
                },
            ],
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
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);

        try
        {
            var updatedDlg = new DlgFile
            {
                Entries =
                [
                    new DialogEntry
                    {
                        Num = 1,
                        Text = "Modified text.",
                        GenderField = string.Empty,
                        Iq = 0,
                        Conditions = string.Empty,
                        ResponseVal = 0,
                        Actions = string.Empty,
                    },
                ],
            };

            SaveGameWriter.Save(
                save,
                tmpDir,
                "testslot",
                updatedDialogs: new Dictionary<string, DlgFile> { ["npc/guard.dlg"] = updatedDlg }
            );

            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            await Assert.That(loaded.Dialogs.ContainsKey("npc/guard.dlg")).IsTrue();
            await Assert.That(loaded.Dialogs["npc/guard.dlg"].Entries[0].Text).IsEqualTo("Modified text.");
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

    // ── ParseErrors ───────────────────────────────────────────────────────────

    [Test]
    public async Task ParseErrors_EmptyForValidSave()
    {
        var (save, _) = MakeMinimalSave();
        await Assert.That(save.ParseErrors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ParseErrors_ContainsEntryForCorruptMobFile()
    {
        // Build a save where the .mob file contains garbage bytes.
        var corruptBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xDE, 0xAD };
        var index = new SaveIndex
        {
            Root =
            [
                new TfaiDirectoryEntry
                {
                    Name = "maps",
                    Children = [new TfaiFileEntry { Name = "corrupt.mob", Size = corruptBytes.Length }],
                },
            ],
        };
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/corrupt.mob"] = corruptBytes,
        };
        var info = new SaveInfo
        {
            ModuleName = "arcanum",
            LeaderName = "Tester",
            DisplayName = "Corrupt Save",
            MapId = 1,
            GameTimeDays = 0,
            GameTimeMs = 0,
            LeaderPortraitId = 0,
            LeaderLevel = 1,
            LeaderTileX = 0,
            LeaderTileY = 0,
            StoryState = 0,
        };
        var tfafBytes = TfafFormat.Pack(index, files);
        var save = SaveGameLoader.LoadFromParsed(info, index, tfafBytes);

        await Assert.That(save.ParseErrors.ContainsKey("maps/corrupt.mob")).IsTrue();
        // Raw bytes must still be accessible even when parsing failed.
        await Assert.That(save.Files.ContainsKey("maps/corrupt.mob")).IsTrue();
        // The corrupt file must NOT appear in the typed Mobiles dictionary.
        await Assert.That(save.Mobiles.ContainsKey("maps/corrupt.mob")).IsFalse();
    }

    [Test]
    public async Task ParseErrors_DoesNotAffectOtherFiles()
    {
        var mobBytes = MobFormat.WriteToArray(MakePc());
        var corruptBytes = new byte[] { 0xDE, 0xAD };

        var index = new SaveIndex
        {
            Root =
            [
                new TfaiDirectoryEntry
                {
                    Name = "maps",
                    Children =
                    [
                        new TfaiFileEntry { Name = "valid.mob", Size = mobBytes.Length },
                        new TfaiFileEntry { Name = "corrupt.mob", Size = corruptBytes.Length },
                    ],
                },
            ],
        };
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/valid.mob"] = mobBytes,
            ["maps/corrupt.mob"] = corruptBytes,
        };
        var info = new SaveInfo
        {
            ModuleName = "arcanum",
            LeaderName = "Tester",
            DisplayName = "Mixed Save",
            MapId = 1,
            GameTimeDays = 0,
            GameTimeMs = 0,
            LeaderPortraitId = 0,
            LeaderLevel = 1,
            LeaderTileX = 0,
            LeaderTileY = 0,
            StoryState = 0,
        };
        var tfafBytes = TfafFormat.Pack(index, files);
        var save = SaveGameLoader.LoadFromParsed(info, index, tfafBytes);

        // The corrupt file records an error.
        await Assert.That(save.ParseErrors.ContainsKey("maps/corrupt.mob")).IsTrue();
        // The valid file is parsed successfully.
        await Assert.That(save.Mobiles.ContainsKey("maps/valid.mob")).IsTrue();
        await Assert.That(save.ParseErrors.ContainsKey("maps/valid.mob")).IsFalse();
    }

    // ── LoadAsync ─────────────────────────────────────────────────────────────

    [Test]
    public async Task LoadAsync_ParsesMobFiles()
    {
        var (save, _) = MakeMinimalSave();
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);

        try
        {
            SaveGameWriter.Save(save, tmpDir, "testslot");
            var loaded = await SaveGameLoader.LoadAsync(tmpDir, "testslot");

            await Assert.That(loaded.Mobiles.ContainsKey("maps/map01/mobile/G_pc.mob")).IsTrue();
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_Progress_ReachesOne()
    {
        var (save, _) = MakeMinimalSave();
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);

        try
        {
            SaveGameWriter.Save(save, tmpDir, "testslot");

            float lastProgress = 0f;
            var progressReachedOne = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var progress = new Progress<float>(v =>
            {
                lastProgress = v;
                if (v >= 1f)
                    progressReachedOne.TrySetResult(true);
            });

            await SaveGameLoader.LoadAsync(tmpDir, "testslot", progress);

            // Allow the Progress<float> callback to fire (it posts to the sync context).
            var completed = await Task.WhenAny(progressReachedOne.Task, Task.Delay(5000));
            await Assert.That(ReferenceEquals(completed, progressReachedOne.Task)).IsTrue();
            await Assert.That(lastProgress).IsEqualTo(1f);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_CancellationToken_ThrowsOperationCanceled()
    {
        var (save, _) = MakeMinimalSave();
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);

        try
        {
            SaveGameWriter.Save(save, tmpDir, "testslot");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await SaveGameLoader.LoadAsync(tmpDir, "testslot", cancellationToken: cts.Token)
            );
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // ── SaveAsync ─────────────────────────────────────────────────────────────

    [Test]
    public async Task SaveAsync_ThenLoad_RoundTrips()
    {
        var (save, _) = MakeMinimalSave();
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);

        try
        {
            await SaveGameWriter.SaveAsync(save, tmpDir, "testslot");
            var loaded = SaveGameLoader.Load(tmpDir, "testslot");

            await Assert.That(loaded.Info.LeaderName).IsEqualTo(save.Info.LeaderName);
            await Assert.That(loaded.Mobiles.ContainsKey("maps/map01/mobile/G_pc.mob")).IsTrue();
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task SaveAsync_WithUpdatedMobile_PersistsChanges()
    {
        var (save, _) = MakeMinimalSave();
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);

        try
        {
            var updatedMob = new CharacterBuilder(save.Mobiles["maps/map01/mobile/G_pc.mob"])
                .WithPlayerName("AsyncPlayer")
                .WithGold(7777)
                .Build();

            await SaveGameWriter.SaveAsync(
                save,
                tmpDir,
                "testslot",
                updatedMobiles: new Dictionary<string, MobData> { ["maps/map01/mobile/G_pc.mob"] = updatedMob }
            );

            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            var mob = loaded.Mobiles["maps/map01/mobile/G_pc.mob"];
            var name = mob.Properties.First(p => p.Field == ObjectField.ObjFPcPlayerName);
            await Assert.That(name.GetString()).IsEqualTo("AsyncPlayer");
            var gold = mob.Properties.First(p => p.Field == ObjectField.ObjFCritterGold);
            await Assert.That(gold.GetInt32()).IsEqualTo(7777);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task SaveAsync_CancellationToken_ThrowsOperationCanceled()
    {
        var (save, _) = MakeMinimalSave();
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);

        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await SaveGameWriter.SaveAsync(save, tmpDir, "testslot", cancellationToken: cts.Token)
            );
        }
        finally
        {
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, recursive: true);
        }
    }
}
