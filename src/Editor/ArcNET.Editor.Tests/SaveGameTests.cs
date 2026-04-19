using System.Buffers.Binary;
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

    private static (LoadedSave save, byte[] tfafBytes) MakeMinimalSave()
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
                new SaveGameUpdates
                {
                    UpdatedMobiles = new Dictionary<string, MobData> { ["maps/map01/mobile/G_pc.mob"] = updatedMob },
                }
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
                updates: new SaveGameUpdates
                {
                    UpdatedDialogs = new Dictionary<string, DlgFile> { ["npc/guard.dlg"] = updatedDlg },
                }
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

            SaveGameWriter.Save(save, tmpDir, "testslot", new SaveGameUpdates { UpdatedInfo = updatedInfo });
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
                new SaveGameUpdates
                {
                    UpdatedMobiles = new Dictionary<string, MobData> { ["maps/map01/mobile/G_pc.mob"] = updatedMob },
                }
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

    // ── Sector ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a minimal save that contains a single .sec file alongside the standard .mob.
    /// Returns the save and the virtual path to the sector file.
    /// </summary>
    private static (LoadedSave save, string secPath) MakeSaveWithSector(int townmapInfo = 0, int aptitudeAdj = 0)
    {
        var mobBytes = MobFormat.WriteToArray(MakePc());
        var sector = new SectorBuilder().WithTownmapInfo(townmapInfo).WithAptitudeAdjustment(aptitudeAdj).Build();
        var secBytes = SectorFormat.WriteToArray(sector);

        const string secPath = "maps/map01/map01.sec";
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = mobBytes,
            [secPath] = secBytes,
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
                                new TfaiFileEntry { Name = "map01.sec", Size = secBytes.Length },
                            ],
                        },
                    ],
                },
            ],
        };
        var info = MakeInfo();
        var tfafBytes = TfafFormat.Pack(index, files);
        return (SaveGameLoader.LoadFromParsed(info, index, tfafBytes), secPath);
    }

    private static SaveInfo MakeInfo() =>
        new()
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

    [Test]
    public async Task LoadFromParsed_ParsesSectorFiles()
    {
        var (save, secPath) = MakeSaveWithSector(townmapInfo: 7);
        await Assert.That(save.Sectors.ContainsKey(secPath)).IsTrue();
        await Assert.That(save.Sectors[secPath].TownmapInfo).IsEqualTo(7);
    }

    [Test]
    public async Task Save_WithUpdatedSector_PersistsChanges()
    {
        var (save, secPath) = MakeSaveWithSector(townmapInfo: 3);
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var updated = new SectorBuilder(save.Sectors[secPath])
                .WithTownmapInfo(99)
                .WithAptitudeAdjustment(5)
                .Build();
            SaveGameWriter.Save(
                save,
                tmpDir,
                "testslot",
                new SaveGameUpdates { UpdatedSectors = new Dictionary<string, Sector> { [secPath] = updated } }
            );
            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            await Assert.That(loaded.Sectors.ContainsKey(secPath)).IsTrue();
            await Assert.That(loaded.Sectors[secPath].TownmapInfo).IsEqualTo(99);
            await Assert.That(loaded.Sectors[secPath].AptitudeAdjustment).IsEqualTo(5);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // ── Script ────────────────────────────────────────────────────────────────

    private static (LoadedSave save, string scrPath) MakeSaveWithScript(string description = "")
    {
        var mobBytes = MobFormat.WriteToArray(MakePc());
        var scr = new ScriptBuilder().WithDescription(description).Build();
        var scrBytes = ScriptFormat.WriteToArray(scr);

        const string scrPath = "maps/map01/map01.scr";
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = mobBytes,
            [scrPath] = scrBytes,
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
                                new TfaiFileEntry { Name = "map01.scr", Size = scrBytes.Length },
                            ],
                        },
                    ],
                },
            ],
        };
        var tfafBytes = TfafFormat.Pack(index, files);
        return (SaveGameLoader.LoadFromParsed(MakeInfo(), index, tfafBytes), scrPath);
    }

    [Test]
    public async Task LoadFromParsed_ParsesScriptFiles()
    {
        var (save, scrPath) = MakeSaveWithScript("TestScript");
        await Assert.That(save.Scripts.ContainsKey(scrPath)).IsTrue();
        await Assert.That(save.Scripts[scrPath].Description).IsEqualTo("TestScript");
    }

    [Test]
    public async Task Save_WithUpdatedScript_PersistsChanges()
    {
        var (save, scrPath) = MakeSaveWithScript("Original");
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var updated = new ScriptBuilder(save.Scripts[scrPath]).WithDescription("Modified").Build();
            SaveGameWriter.Save(
                save,
                tmpDir,
                "testslot",
                new SaveGameUpdates { UpdatedScripts = new Dictionary<string, ScrFile> { [scrPath] = updated } }
            );
            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            await Assert.That(loaded.Scripts.ContainsKey(scrPath)).IsTrue();
            await Assert.That(loaded.Scripts[scrPath].Description).IsEqualTo("Modified");
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // ── MapProperties ─────────────────────────────────────────────────────────

    private static (LoadedSave save, string prpPath) MakeSaveWithMapProperties(int artId = 1)
    {
        var mobBytes = MobFormat.WriteToArray(MakePc());
        var props = new MapProperties
        {
            ArtId = artId,
            Unused = 0,
            LimitX = 960,
            LimitY = 960,
        };
        var prpBytes = MapPropertiesFormat.WriteToArray(props);

        const string prpPath = "maps/map01/map01.prp";
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = mobBytes,
            [prpPath] = prpBytes,
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
                                new TfaiFileEntry { Name = "map01.prp", Size = prpBytes.Length },
                            ],
                        },
                    ],
                },
            ],
        };
        var tfafBytes = TfafFormat.Pack(index, files);
        return (SaveGameLoader.LoadFromParsed(MakeInfo(), index, tfafBytes), prpPath);
    }

    [Test]
    public async Task LoadFromParsed_ParsesMapPropertiesFiles()
    {
        var (save, prpPath) = MakeSaveWithMapProperties(artId: 42);
        await Assert.That(save.MapPropertiesList.ContainsKey(prpPath)).IsTrue();
        await Assert.That(save.MapPropertiesList[prpPath].ArtId).IsEqualTo(42);
    }

    [Test]
    public async Task Save_WithUpdatedMapProperties_PersistsChanges()
    {
        var (save, prpPath) = MakeSaveWithMapProperties(artId: 1);
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var original = save.MapPropertiesList[prpPath];
            var updated = new MapProperties
            {
                ArtId = 77,
                Unused = original.Unused,
                LimitX = original.LimitX,
                LimitY = original.LimitY,
            };
            SaveGameWriter.Save(
                save,
                tmpDir,
                "testslot",
                new SaveGameUpdates
                {
                    UpdatedMapProperties = new Dictionary<string, MapProperties> { [prpPath] = updated },
                }
            );
            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            await Assert.That(loaded.MapPropertiesList.ContainsKey(prpPath)).IsTrue();
            await Assert.That(loaded.MapPropertiesList[prpPath].ArtId).IsEqualTo(77);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // ── MessageFile ──────────────────────────────────────────────────────────

    private static (LoadedSave save, string messagePath) MakeSaveWithMessageFile(MesFile? message = null)
    {
        var mobBytes = MobFormat.WriteToArray(MakePc());
        var mesFile =
            message ?? new MesFile { Entries = [new MessageEntry(10, "Alpha"), new MessageEntry(20, "Beta")] };
        var messageBytes = MessageFormat.WriteToArray(in mesFile);

        const string messagePath = "modules/Arcanum/Arcanum.mes";
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = mobBytes,
            [messagePath] = messageBytes,
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
                            Name = "Arcanum",
                            Children = [new TfaiFileEntry { Name = "Arcanum.mes", Size = messageBytes.Length }],
                        },
                    ],
                },
            ],
        };
        var tfafBytes = TfafFormat.Pack(index, files);
        return (SaveGameLoader.LoadFromParsed(MakeInfo(), index, tfafBytes), messagePath);
    }

    [Test]
    public async Task LoadFromParsed_ParsesMessageFiles()
    {
        var (save, messagePath) = MakeSaveWithMessageFile();

        await Assert.That(save.Messages.ContainsKey(messagePath)).IsTrue();
        await Assert.That(save.Messages[messagePath].Entries.Count).IsEqualTo(2);
        await Assert.That(save.Messages[messagePath].Entries[1].Text).IsEqualTo("Beta");
        await Assert.That(save.RawFiles.ContainsKey(messagePath)).IsFalse();
    }

    [Test]
    public async Task Save_WithUpdatedMessageFile_PersistsChanges()
    {
        var (save, messagePath) = MakeSaveWithMessageFile();
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var updated = new MesFile { Entries = [new MessageEntry(10, "Alpha+"), new MessageEntry(30, "Gamma")] };
            SaveGameWriter.Save(
                save,
                tmpDir,
                "testslot",
                new SaveGameUpdates { UpdatedMessages = new Dictionary<string, MesFile> { [messagePath] = updated } }
            );

            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            await Assert.That(loaded.Messages.ContainsKey(messagePath)).IsTrue();
            await Assert.That(loaded.Messages[messagePath].Entries.Count).IsEqualTo(2);
            await Assert.That(loaded.Messages[messagePath].Entries[0].Text).IsEqualTo("Alpha+");
            await Assert.That(loaded.Messages[messagePath].Entries[1].Index).IsEqualTo(30);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // ── TownMapFog ───────────────────────────────────────────────────────────

    private static (LoadedSave save, string tmfPath) MakeSaveWithTownMapFog(byte[]? rawBytes = null)
    {
        var mobBytes = MobFormat.WriteToArray(MakePc());
        var fog = new TownMapFog { RawBytes = rawBytes ?? [0x03, 0x80] };
        var tmfBytes = TownMapFogFormat.WriteToArray(in fog);

        const string tmfPath = "Tsen Ang.tmf";
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = mobBytes,
            [tmfPath] = tmfBytes,
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
                new TfaiFileEntry { Name = tmfPath, Size = tmfBytes.Length },
            ],
        };
        var tfafBytes = TfafFormat.Pack(index, files);
        return (SaveGameLoader.LoadFromParsed(MakeInfo(), index, tfafBytes), tmfPath);
    }

    private static (LoadedSave save, string dataSavPath) MakeSaveWithDataSavFile(byte[]? rawBytes = null)
    {
        var mobBytes = MobFormat.WriteToArray(MakePc());
        var dataSavBytes = rawBytes ?? BuildDataSavBytes();

        const string dataSavPath = "data.sav";
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = mobBytes,
            [dataSavPath] = dataSavBytes,
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
                new TfaiFileEntry { Name = dataSavPath, Size = dataSavBytes.Length },
            ],
        };
        var tfafBytes = TfafFormat.Pack(index, files);
        return (SaveGameLoader.LoadFromParsed(MakeInfo(), index, tfafBytes), dataSavPath);
    }

    private static (LoadedSave save, string data2Path) MakeSaveWithData2SavFile(byte[]? rawBytes = null)
    {
        var mobBytes = MobFormat.WriteToArray(MakePc());
        var data2Bytes = rawBytes ?? BuildData2SavBytes();

        const string data2Path = "data2.sav";
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = mobBytes,
            [data2Path] = data2Bytes,
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
                new TfaiFileEntry { Name = data2Path, Size = data2Bytes.Length },
            ],
        };
        var tfafBytes = TfafFormat.Pack(index, files);
        return (SaveGameLoader.LoadFromParsed(MakeInfo(), index, tfafBytes), data2Path);
    }

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

    [Test]
    public async Task LoadFromParsed_ParsesTownMapFogFiles()
    {
        var (save, tmfPath) = MakeSaveWithTownMapFog();

        await Assert.That(save.TownMapFogs.ContainsKey(tmfPath)).IsTrue();
        await Assert.That(save.TownMapFogs[tmfPath].RevealedTiles).IsEqualTo(3);
    }

    [Test]
    public async Task Save_WithUpdatedTownMapFog_PersistsChanges()
    {
        var (save, tmfPath) = MakeSaveWithTownMapFog([0x01]);
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var updated = new TownMapFog { RawBytes = [0xFF, 0x00, 0x01] };
            SaveGameWriter.Save(
                save,
                tmpDir,
                "testslot",
                new SaveGameUpdates { UpdatedTownMapFogs = new Dictionary<string, TownMapFog> { [tmfPath] = updated } }
            );

            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            await Assert.That(loaded.TownMapFogs.ContainsKey(tmfPath)).IsTrue();
            await Assert.That(loaded.TownMapFogs[tmfPath].RawBytes.SequenceEqual(updated.RawBytes)).IsTrue();
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // ── data.sav ─────────────────────────────────────────────────────────────

    [Test]
    public async Task LoadFromParsed_ParsesDataSavFiles()
    {
        var (save, dataSavPath) = MakeSaveWithDataSavFile();

        await Assert.That(save.DataSavFiles.ContainsKey(dataSavPath)).IsTrue();
        await Assert.That(save.DataSavFiles[dataSavPath].Header0).IsEqualTo(25);
        await Assert.That(save.DataSavFiles[dataSavPath].Header1).IsEqualTo(32);
        await Assert
            .That(save.DataSavFiles[dataSavPath].GetQuadRow(0))
            .IsEqualTo(new DataSavQuadRow(7, 18, 2072, 0x02441780));
        await Assert.That(save.RawFiles.ContainsKey(dataSavPath)).IsFalse();
    }

    [Test]
    public async Task Save_WithUpdatedDataSavFile_PersistsChanges()
    {
        var (save, dataSavPath) = MakeSaveWithDataSavFile();
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var updated = save.DataSavFiles[dataSavPath]
                .WithHeader(25, 31)
                .WithQuadRow(1, new DataSavQuadRow(26, 7, 2072, 0x021CE520))
                .WithRemainderInt(1, 999);
            SaveGameWriter.Save(
                save,
                tmpDir,
                "testslot",
                new SaveGameUpdates
                {
                    UpdatedDataSavFiles = new Dictionary<string, DataSavFile> { [dataSavPath] = updated },
                }
            );

            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            await Assert.That(loaded.DataSavFiles.ContainsKey(dataSavPath)).IsTrue();
            await Assert.That(loaded.DataSavFiles[dataSavPath].Header1).IsEqualTo(31);
            await Assert
                .That(loaded.DataSavFiles[dataSavPath].GetQuadRow(1))
                .IsEqualTo(new DataSavQuadRow(26, 7, 2072, 0x021CE520));
            await Assert.That(loaded.DataSavFiles[dataSavPath].GetRemainderInt(1)).IsEqualTo(999);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // ── data2.sav ────────────────────────────────────────────────────────────

    [Test]
    public async Task LoadFromParsed_ParsesData2SavFiles()
    {
        var (save, data2Path) = MakeSaveWithData2SavFile();

        await Assert.That(save.Data2SavFiles.ContainsKey(data2Path)).IsTrue();
        await Assert.That(save.Data2SavFiles[data2Path].TryGetIdPairValue(50005, out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(5);
        await Assert.That(save.RawFiles.ContainsKey(data2Path)).IsFalse();
    }

    [Test]
    public async Task Save_WithUpdatedData2SavFile_PersistsChanges()
    {
        var (save, data2Path) = MakeSaveWithData2SavFile();
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var updated = save.Data2SavFiles[data2Path]
                .WithIdPairValue(50005, 17)
                .WithPrefixInt(5, 11)
                .WithSuffixInt(1, 27);
            SaveGameWriter.Save(
                save,
                tmpDir,
                "testslot",
                new SaveGameUpdates
                {
                    UpdatedData2SavFiles = new Dictionary<string, Data2SavFile> { [data2Path] = updated },
                }
            );

            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            await Assert.That(loaded.Data2SavFiles.ContainsKey(data2Path)).IsTrue();
            await Assert.That(loaded.Data2SavFiles[data2Path].TryGetIdPairValue(50005, out var value)).IsTrue();
            await Assert.That(value).IsEqualTo(17);
            await Assert.That(loaded.Data2SavFiles[data2Path].GetPrefixInt(5)).IsEqualTo(11);
            await Assert.That(loaded.Data2SavFiles[data2Path].GetSuffixInt(1)).IsEqualTo(27);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // ── JumpFile ──────────────────────────────────────────────────────────────

    private static (LoadedSave save, string jmpPath) MakeSaveWithJump(int destMapId = 1)
    {
        var mobBytes = MobFormat.WriteToArray(MakePc());
        var jmpFile = new JmpFile
        {
            Jumps =
            [
                new JumpEntry
                {
                    Flags = 0,
                    SourceLoc = 0,
                    DestinationMapId = destMapId,
                    DestinationLoc = 0,
                },
            ],
        };
        var jmpBytes = JmpFormat.WriteToArray(jmpFile);

        const string jmpPath = "maps/map01/map01.jmp";
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = mobBytes,
            [jmpPath] = jmpBytes,
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
                                new TfaiFileEntry { Name = "map01.jmp", Size = jmpBytes.Length },
                            ],
                        },
                    ],
                },
            ],
        };
        var tfafBytes = TfafFormat.Pack(index, files);
        return (SaveGameLoader.LoadFromParsed(MakeInfo(), index, tfafBytes), jmpPath);
    }

    [Test]
    public async Task LoadFromParsed_ParsesJumpFiles()
    {
        var (save, jmpPath) = MakeSaveWithJump(destMapId: 5);
        await Assert.That(save.JumpFiles.ContainsKey(jmpPath)).IsTrue();
        await Assert.That(save.JumpFiles[jmpPath].Jumps.Count).IsEqualTo(1);
        await Assert.That(save.JumpFiles[jmpPath].Jumps[0].DestinationMapId).IsEqualTo(5);
    }

    [Test]
    public async Task Save_WithUpdatedJumpFile_PersistsChanges()
    {
        var (save, jmpPath) = MakeSaveWithJump(destMapId: 1);
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var src = save.JumpFiles[jmpPath].Jumps[0];
            var updated = new JmpFile
            {
                Jumps =
                [
                    new JumpEntry
                    {
                        Flags = src.Flags,
                        SourceLoc = src.SourceLoc,
                        DestinationMapId = 99,
                        DestinationLoc = src.DestinationLoc,
                    },
                ],
            };
            SaveGameWriter.Save(
                save,
                tmpDir,
                "testslot",
                new SaveGameUpdates { UpdatedJumpFiles = new Dictionary<string, JmpFile> { [jmpPath] = updated } }
            );
            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            await Assert.That(loaded.JumpFiles.ContainsKey(jmpPath)).IsTrue();
            await Assert.That(loaded.JumpFiles[jmpPath].Jumps[0].DestinationMapId).IsEqualTo(99);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // ── MobileMd ──────────────────────────────────────────────────────────────

    private static (LoadedSave save, string mdPath) MakeSaveWithMobileMd()
    {
        var mobBytes = MobFormat.WriteToArray(MakePc());
        // An empty mobile.md is valid: no modified objects.
        var mdFile = new MobileMdFile { Records = [] };
        var mdBytes = MobileMdFormat.WriteToArray(mdFile);

        const string mdPath = "maps/map01/mobile.md";
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = mobBytes,
            [mdPath] = mdBytes,
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
                                new TfaiFileEntry { Name = "mobile.md", Size = mdBytes.Length },
                            ],
                        },
                    ],
                },
            ],
        };
        var tfafBytes = TfafFormat.Pack(index, files);
        return (SaveGameLoader.LoadFromParsed(MakeInfo(), index, tfafBytes), mdPath);
    }

    [Test]
    public async Task LoadFromParsed_ParsesMobileMdFiles()
    {
        var (save, mdPath) = MakeSaveWithMobileMd();
        await Assert.That(save.MobileMds.ContainsKey(mdPath)).IsTrue();
        await Assert.That(save.MobileMds[mdPath].Records.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Save_WithUpdatedMobileMd_PersistsChanges()
    {
        var (save, mdPath) = MakeSaveWithMobileMd();

        // Replace with a still-empty file to verify the round-trip path works.
        var updated = new MobileMdFile { Records = [] };
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            SaveGameWriter.Save(
                save,
                tmpDir,
                "testslot",
                new SaveGameUpdates { UpdatedMobileMds = new Dictionary<string, MobileMdFile> { [mdPath] = updated } }
            );
            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            await Assert.That(loaded.MobileMds.ContainsKey(mdPath)).IsTrue();
            await Assert.That(loaded.MobileMds[mdPath].Records.Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // ── MobileMdy ─────────────────────────────────────────────────────────────

    private static (LoadedSave save, string mdyPath) MakeSaveWithMobileMdy()
    {
        var mobBytes = MobFormat.WriteToArray(MakePc());
        // A minimal mobile.mdy containing a single standard mob record.
        var protoId = new GameObjectGuid(1, 0, 0, Guid.Empty);
        var objectId = new GameObjectGuid(3, 0, 1, Guid.Empty);
        var innerMob = new CharacterBuilder(ObjectType.Npc, objectId, protoId).WithHitPoints(50).Build();
        var mdyFile = new MobileMdyFile { Records = [MobileMdyRecord.FromMob(innerMob)] };
        var mdyBytes = MobileMdyFormat.WriteToArray(mdyFile);

        const string mdyPath = "maps/map01/mobile.mdy";
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = mobBytes,
            [mdyPath] = mdyBytes,
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
                                new TfaiFileEntry { Name = "mobile.mdy", Size = mdyBytes.Length },
                            ],
                        },
                    ],
                },
            ],
        };
        var tfafBytes = TfafFormat.Pack(index, files);
        return (SaveGameLoader.LoadFromParsed(MakeInfo(), index, tfafBytes), mdyPath);
    }

    [Test]
    public async Task LoadFromParsed_ParsesMobileMdyFiles()
    {
        var (save, mdyPath) = MakeSaveWithMobileMdy();
        await Assert.That(save.MobileMdys.ContainsKey(mdyPath)).IsTrue();
        await Assert.That(save.MobileMdys[mdyPath].Records.Count).IsEqualTo(1);
        await Assert.That(save.MobileMdys[mdyPath].Records[0].IsMob).IsTrue();
    }

    [Test]
    public async Task Save_WithUpdatedMobileMdy_PersistsChanges()
    {
        var (save, mdyPath) = MakeSaveWithMobileMdy();

        // Wrap the same mob in a new MobileMdyFile — verifies the write path.
        var originalMob = save.MobileMdys[mdyPath].Records[0].Mob!;
        var updatedMob = new CharacterBuilder(originalMob).WithHitPoints(100).Build();
        var updated = new MobileMdyFile { Records = [MobileMdyRecord.FromMob(updatedMob)] };

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            SaveGameWriter.Save(
                save,
                tmpDir,
                "testslot",
                new SaveGameUpdates
                {
                    UpdatedMobileMdys = new Dictionary<string, MobileMdyFile> { [mdyPath] = updated },
                }
            );
            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            await Assert.That(loaded.MobileMdys.ContainsKey(mdyPath)).IsTrue();
            await Assert.That(loaded.MobileMdys[mdyPath].Records.Count).IsEqualTo(1);
            var mob = loaded.MobileMdys[mdyPath].Records[0].Mob!;
            var hpProp = mob.Properties.First(p => p.Field == ObjectField.ObjFHpPts);
            await Assert.That(hpProp.GetInt32()).IsEqualTo(100);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // ── Raw file update ───────────────────────────────────────────────────────

    [Test]
    public async Task Save_WithRawFileUpdate_ReplacesPayload()
    {
        var (save, _) = MakeMinimalSave();

        // Use the existing jmp file path (present from MakeMinimalSave) and replace
        // its bytes with a known raw payload that is NOT a valid JMP, so we can
        // distinguish it from the original bytes.
        const string rawPath = "maps/map01/map.jmp";
        var sentinel = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            SaveGameWriter.Save(
                save,
                tmpDir,
                "testslot",
                new SaveGameUpdates { RawFileUpdates = new Dictionary<string, byte[]> { [rawPath] = sentinel } }
            );
            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            await Assert.That(loaded.Files.ContainsKey(rawPath)).IsTrue();
            await Assert.That(loaded.Files[rawPath].SequenceEqual(sentinel)).IsTrue();
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_RawFileUpdate_TakesHigherPrecedenceThanTypedUpdate()
    {
        // Build a save with a JMP file, update it via both the typed path and raw bytes.
        // The raw update must win (it is applied last in Serialize()).
        var (save, jmpPath) = MakeSaveWithJump(destMapId: 1);
        var rawOverride = new byte[] { 0xCA, 0xFE };

        var srcJump = save.JumpFiles[jmpPath].Jumps[0];
        var updatedJmp = new JmpFile
        {
            Jumps =
            [
                new JumpEntry
                {
                    Flags = srcJump.Flags,
                    SourceLoc = srcJump.SourceLoc,
                    DestinationMapId = 50,
                    DestinationLoc = srcJump.DestinationLoc,
                },
            ],
        };

        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            SaveGameWriter.Save(
                save,
                tmpDir,
                "testslot",
                new SaveGameUpdates
                {
                    UpdatedJumpFiles = new Dictionary<string, JmpFile> { [jmpPath] = updatedJmp },
                    RawFileUpdates = new Dictionary<string, byte[]> { [jmpPath] = rawOverride },
                }
            );
            var loaded = SaveGameLoader.Load(tmpDir, "testslot");
            // rawOverride bytes — NOT the typed JmpFile bytes.
            await Assert.That(loaded.Files[jmpPath].SequenceEqual(rawOverride)).IsTrue();
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // ── RebuildIndex — additional file types ──────────────────────────────────

    [Test]
    public async Task RebuildIndex_UpdatedSector_ReflectsNewSize()
    {
        var (save, secPath) = MakeSaveWithSector();
        var bigger = new SectorBuilder(save.Sectors[secPath]).WithAptitudeAdjustment(999).Build();
        var biggerBytes = SectorFormat.WriteToArray(bigger);

        var updatedFiles = new Dictionary<string, byte[]>(save.Files, StringComparer.OrdinalIgnoreCase)
        {
            [secPath] = biggerBytes,
        };
        var rebuilt = SaveGameWriter.RebuildIndex(save.Index, updatedFiles);

        // Flatten to find the sector entry.
        int? FindSize(IReadOnlyList<TfaiEntry> entries, string name)
        {
            foreach (var e in entries)
            {
                if (e is TfaiFileEntry f && f.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return f.Size;
                if (e is TfaiDirectoryEntry d)
                {
                    var r = FindSize(d.Children, name);
                    if (r.HasValue)
                        return r;
                }
            }
            return null;
        }

        var size = FindSize(rebuilt.Root, "map01.sec");
        await Assert.That(size).IsEqualTo(biggerBytes.Length);
    }
}
