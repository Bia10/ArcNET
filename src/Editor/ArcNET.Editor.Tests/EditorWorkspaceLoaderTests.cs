using System.Buffers.Binary;
using ArcNET.Archive;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor.Tests;

public class EditorWorkspaceLoaderTests
{
    private static GameObjectGuid MakeProtoId(int protoNumber)
    {
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, protoNumber);
        return new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, new Guid(bytes));
    }

    private static GameObjectScript MakeScript(int scriptId) => new(Counters: 0u, Flags: 0, ScriptId: scriptId);

    private static ScrFile MakeScriptFile(string description = "Test script", int conditionCount = 1) =>
        new()
        {
            HeaderFlags = 0,
            HeaderCounters = 0,
            Description = description,
            Flags = 0,
            Entries = Enumerable
                .Range(0, conditionCount)
                .Select(static _ => new ScriptConditionData(
                    1,
                    default,
                    default,
                    new ScriptActionData(0, default, default),
                    new ScriptActionData(0, default, default)
                ))
                .ToArray(),
        };

    private static DlgFile MakeDialogFile(params (int Num, string Text, int ResponseVal)[] entries) =>
        new()
        {
            Entries = entries
                .Select(entry => new DialogEntry
                {
                    Num = entry.Num,
                    Text = entry.Text,
                    GenderField = string.Empty,
                    Iq = 0,
                    Conditions = string.Empty,
                    ResponseVal = entry.ResponseVal,
                    Actions = string.Empty,
                })
                .ToArray(),
        };

    private static ObjectProperty MakeScriptProperty(params int[] scriptIds) =>
        new ObjectProperty { Field = ObjectField.ObjFScriptsIdx, RawBytes = [0] }.WithScriptArray([
            .. scriptIds.Select(scriptId => new ObjectPropertyScript(0u, 0u, scriptId)),
        ]);

    private static ObjectProperty MakeArtProperty(ObjectField field, uint artId) =>
        ObjectPropertyFactory.ForInt32(field, unchecked((int)artId));

    private static MobData WithProperties(MobData mob, params ObjectProperty[] properties)
    {
        byte[] bitmap = [.. mob.Header.Bitmap];
        foreach (var property in properties)
            bitmap.SetField(property.Field, true);

        return new MobData
        {
            Header = new GameObjectHeader
            {
                Version = mob.Header.Version,
                ProtoId = mob.Header.ProtoId,
                ObjectId = mob.Header.ObjectId,
                GameObjectType = mob.Header.GameObjectType,
                PropCollectionItems = mob.Header.PropCollectionItems,
                Bitmap = bitmap,
            },
            Properties = [.. mob.Properties.Concat(properties).OrderBy(static property => property.Field)],
        };
    }

    private static ProtoData WithProperties(ProtoData proto, params ObjectProperty[] properties)
    {
        byte[] bitmap = [.. proto.Header.Bitmap];
        foreach (var property in properties)
            bitmap.SetField(property.Field, true);

        return new ProtoData
        {
            Header = new GameObjectHeader
            {
                Version = proto.Header.Version,
                ProtoId = proto.Header.ProtoId,
                ObjectId = proto.Header.ObjectId,
                GameObjectType = proto.Header.GameObjectType,
                PropCollectionItems = proto.Header.PropCollectionItems,
                Bitmap = bitmap,
            },
            Properties = [.. proto.Properties.Concat(properties).OrderBy(static property => property.Field)],
        };
    }

    private static MobData MakePc(int protoNumber = 1)
    {
        var protoId = MakeProtoId(protoNumber);
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid());
        return new CharacterBuilder(ObjectType.Pc, objectId, protoId)
            .WithPlayerName("WorkspacePc")
            .WithHitPoints(80)
            .Build();
    }

    private static ProtoData MakeProto(int protoNumber)
    {
        var mob = MakePc(protoNumber);
        return new ProtoData
        {
            Header = new GameObjectHeader
            {
                Version = mob.Header.Version,
                ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeBlocked, 0, 0, Guid.Empty),
                ObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid()),
                GameObjectType = mob.Header.GameObjectType,
                PropCollectionItems = 0,
                Bitmap = [.. mob.Header.Bitmap],
            },
            Properties = [.. mob.Properties],
        };
    }

    private static Sector MakeSector(params MobData[] objects) =>
        new()
        {
            Lights = [],
            Tiles = new uint[4096],
            HasRoofs = false,
            Roofs = null,
            SectorScript = null,
            TileScripts = [],
            TownmapInfo = 0,
            AptitudeAdjustment = 0,
            LightSchemeIdx = 0,
            SoundList = SectorSoundList.Default,
            BlockMask = new uint[128],
            Objects = objects,
        };

    private static LoadedSave MakeMinimalSave()
    {
        var mobBytes = MobFormat.WriteToArray(MakePc());
        var jmpBytes = JmpFormat.WriteToArray(new JmpFile { Jumps = [] });

        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = mobBytes,
            ["maps/map01/map.jmp"] = jmpBytes,
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
                                new TfaiFileEntry { Name = "map.jmp", Size = jmpBytes.Length },
                            ],
                        },
                    ],
                },
            ],
        };

        var info = new SaveInfo
        {
            ModuleName = "arcanum",
            LeaderName = "WorkspacePc",
            DisplayName = "Workspace Test Save",
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

    private static async Task WriteDatAsync(string archivePath, IReadOnlyDictionary<string, byte[]> entries)
    {
        var inputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(inputDir);

        try
        {
            foreach (var (virtualPath, bytes) in entries)
            {
                var fullPath = Path.Combine(
                    inputDir,
                    virtualPath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)
                );
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                await File.WriteAllBytesAsync(fullPath, bytes);
            }

            await DatPacker.PackAsync(inputDir, archivePath);
        }
        finally
        {
            if (Directory.Exists(inputDir))
                Directory.Delete(inputDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_LoadsLooseGameDataWithoutSave()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));

        try
        {
            var mes = new MesFile { Entries = [new MessageEntry(10, "Hello from workspace")] };
            var mesPath = Path.Combine(contentDir, "mes", "game.mes");
            MessageFormat.WriteToFile(in mes, mesPath);

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var asset = workspace.Assets.Find("mes/game.mes");

            await Assert.That(workspace.ContentDirectory).IsEqualTo(contentDir);
            await Assert.That(workspace.GameData.Messages.Count).IsEqualTo(1);
            await Assert.That(workspace.Assets.Count).IsEqualTo(1);
            await Assert.That(asset).IsNotNull();
            await Assert.That(asset!.SourceKind).IsEqualTo(EditorAssetSourceKind.LooseFile);
            await Assert.That(asset.SourcePath).IsEqualTo(mesPath);
            await Assert.That(asset.SourceEntryPath).IsNull();
            await Assert.That(workspace.LoadReport.HasSkippedInputs).IsFalse();
            await Assert.That(workspace.Validation.HasIssues).IsFalse();
            await Assert.That(workspace.Validation.Issues.Count).IsEqualTo(0);
            await Assert.That(workspace.HasSaveLoaded).IsFalse();
            await Assert.That(workspace.Save).IsNull();
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_LoadsScriptAndDialogAssetsAndIndexesDefinitions()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(
                MakeScriptFile("Workspace script A", conditionCount: 2),
                Path.Combine(contentDir, "scr", "00777Alpha.scr")
            );
            ScriptFormat.WriteToFile(
                MakeScriptFile("Workspace script B", conditionCount: 1),
                Path.Combine(contentDir, "scr", "00777Beta.scr")
            );
            DialogFormat.WriteToFile(
                MakeDialogFile((1, "Hello there", 2), (2, "Goodbye", 0)),
                Path.Combine(contentDir, "dlg", "00123Alpha.dlg")
            );
            DialogFormat.WriteToFile(
                MakeDialogFile((10, "Another branch", 0)),
                Path.Combine(contentDir, "dlg", "00123Beta.dlg")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);

            var scriptAsset = workspace.Assets.Find("scr/00777Alpha.scr");
            var dialogAsset = workspace.Assets.Find("dlg/00123Alpha.dlg");
            var scriptDefinitions = workspace.Index.FindScriptDefinitions(777);
            var dialogDefinitions = workspace.Index.FindDialogDefinitions(123);

            await Assert.That(workspace.GameData.Scripts.Count).IsEqualTo(2);
            await Assert.That(workspace.GameData.Dialogs.Count).IsEqualTo(2);
            await Assert.That(scriptAsset).IsNotNull();
            await Assert.That(dialogAsset).IsNotNull();
            await Assert.That(scriptDefinitions.Count).IsEqualTo(2);
            await Assert.That(dialogDefinitions.Count).IsEqualTo(2);
            await Assert.That(scriptDefinitions[0].AssetPath).IsEqualTo("scr/00777Alpha.scr");
            await Assert.That(scriptDefinitions[1].AssetPath).IsEqualTo("scr/00777Beta.scr");
            await Assert.That(dialogDefinitions[0].AssetPath).IsEqualTo("dlg/00123Alpha.dlg");
            await Assert.That(dialogDefinitions[1].AssetPath).IsEqualTo("dlg/00123Beta.dlg");
            await Assert.That(workspace.Index.FindScriptDefinition(777)?.AssetPath).IsEqualTo("scr/00777Alpha.scr");
            await Assert.That(workspace.Index.FindDialogDefinition(123)?.AssetPath).IsEqualTo("dlg/00123Alpha.dlg");
            await Assert.That(workspace.Index.FindScriptDefinitions(778).Count).IsEqualTo(0);
            await Assert.That(workspace.Index.FindDialogDefinitions(124).Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_BuildsDialogAndScriptSemanticSummaries()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(
                MakeScriptFile("Semantic script A", conditionCount: 2),
                Path.Combine(contentDir, "scr", "00001Alpha.scr")
            );
            ScriptFormat.WriteToFile(
                MakeScriptFile("Semantic script B", conditionCount: 1),
                Path.Combine(contentDir, "scr", "00001Beta.scr")
            );
            DialogFormat.WriteToFile(
                new DlgFile
                {
                    Entries =
                    [
                        new DialogEntry
                        {
                            Num = 10,
                            Text = "Hello",
                            GenderField = string.Empty,
                            Iq = 0,
                            Conditions = string.Empty,
                            ResponseVal = 20,
                            Actions = string.Empty,
                        },
                        new DialogEntry
                        {
                            Num = 20,
                            Text = "Ask",
                            GenderField = string.Empty,
                            Iq = 8,
                            Conditions = string.Empty,
                            ResponseVal = 30,
                            Actions = string.Empty,
                        },
                        new DialogEntry
                        {
                            Num = 30,
                            Text = "E:",
                            GenderField = string.Empty,
                            Iq = 0,
                            Conditions = string.Empty,
                            ResponseVal = 0,
                            Actions = string.Empty,
                        },
                        new DialogEntry
                        {
                            Num = 35,
                            Text = "Special",
                            GenderField = string.Empty,
                            Iq = 0,
                            Conditions = string.Empty,
                            ResponseVal = -1,
                            Actions = string.Empty,
                        },
                        new DialogEntry
                        {
                            Num = 40,
                            Text = "Orphan",
                            GenderField = string.Empty,
                            Iq = 0,
                            Conditions = string.Empty,
                            ResponseVal = 999,
                            Actions = string.Empty,
                        },
                    ],
                },
                Path.Combine(contentDir, "dlg", "00001Alpha.dlg")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);

            var scriptDetails = workspace.Index.FindScriptDetails(1);
            var dialogDetails = workspace.Index.FindDialogDetails(1);

            await Assert.That(scriptDetails.Count).IsEqualTo(2);
            await Assert.That(scriptDetails[0].Asset.AssetPath).IsEqualTo("scr/00001Alpha.scr");
            await Assert.That(scriptDetails[0].Description).IsEqualTo("Semantic script A");
            await Assert.That(scriptDetails[0].EntryCount).IsEqualTo(2);
            await Assert.That(scriptDetails[0].ActiveAttachmentCount).IsEqualTo(2);
            await Assert.That(scriptDetails[0].ActiveAttachmentSlots.SequenceEqual([0, 1])).IsTrue();
            await Assert
                .That(
                    scriptDetails[0]
                        .ActiveAttachmentPoints.SequenceEqual([
                            ScriptAttachmentPoint.Examine,
                            ScriptAttachmentPoint.Use,
                        ])
                )
                .IsTrue();
            await Assert.That(scriptDetails[1].ActiveAttachmentCount).IsEqualTo(1);

            await Assert.That(dialogDetails.Count).IsEqualTo(1);
            await Assert.That(dialogDetails[0].Asset.AssetPath).IsEqualTo("dlg/00001Alpha.dlg");
            await Assert.That(dialogDetails[0].EntryCount).IsEqualTo(5);
            await Assert.That(dialogDetails[0].NpcEntryCount).IsEqualTo(4);
            await Assert.That(dialogDetails[0].PcOptionCount).IsEqualTo(1);
            await Assert.That(dialogDetails[0].ControlEntryCount).IsEqualTo(1);
            await Assert.That(dialogDetails[0].TransitionCount).IsEqualTo(3);
            await Assert.That(dialogDetails[0].TerminalEntryCount).IsEqualTo(1);
            await Assert.That(dialogDetails[0].RootEntryNumbers.SequenceEqual([10, 35, 40])).IsTrue();
            await Assert.That(dialogDetails[0].MissingResponseTargetNumbers.SequenceEqual([999])).IsTrue();
            await Assert.That(dialogDetails[0].HasMissingResponseTargets).IsTrue();
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_BuildsWorkspaceValidationFindingsForBrokenReferences()
    {
        const int protoNumber = 1001;
        const int scriptId = 777;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mob"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            var mob = WithProperties(MakePc(protoNumber), MakeScriptProperty(scriptId));
            MobFormat.WriteToFile(mob, Path.Combine(contentDir, "mob", "broken.mob"));
            DialogFormat.WriteToFile(
                MakeDialogFile((1, "Hello", 999)),
                Path.Combine(contentDir, "dlg", "00001Broken.dlg")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var issues = workspace.Validation.Issues;

            await Assert.That(workspace.Validation.HasIssues).IsTrue();
            await Assert.That(workspace.Validation.HasErrors).IsTrue();
            await Assert.That(issues.Count).IsEqualTo(3);

            var protoIssue = issues.Single(issue =>
                issue.Severity == EditorWorkspaceValidationSeverity.Error && issue.AssetPath == "mob/broken.mob"
            );
            var scriptIssue = issues.Single(issue =>
                issue.Severity == EditorWorkspaceValidationSeverity.Warning
                && issue.AssetPath == "mob/broken.mob"
                && issue.Message.Contains("script 777", StringComparison.Ordinal)
            );
            var dialogIssue = issues.Single(issue =>
                issue.Severity == EditorWorkspaceValidationSeverity.Warning && issue.AssetPath == "dlg/00001Broken.dlg"
            );

            await Assert.That(protoIssue.Message.Contains("proto 1001", StringComparison.Ordinal)).IsTrue();
            await Assert.That(scriptIssue.Message.Contains("script 777", StringComparison.Ordinal)).IsTrue();
            await Assert.That(dialogIssue.Message.Contains("999", StringComparison.Ordinal)).IsTrue();
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_BuildsWorkspaceValidationInfoForUnknownScriptAttachmentSlots()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));

        try
        {
            ScriptFormat.WriteToFile(
                MakeScriptFile("Unknown attachment slot", conditionCount: 37),
                Path.Combine(contentDir, "scr", "00002Unknown.scr")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var issue = workspace.Validation.Issues.Single();

            await Assert.That(issue.Severity).IsEqualTo(EditorWorkspaceValidationSeverity.Info);
            await Assert.That(issue.AssetPath).IsEqualTo("scr/00002Unknown.scr");
            await Assert.That(issue.Message.Contains("36", StringComparison.Ordinal)).IsTrue();
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadFromGameInstallAsync_BuildsWorkspaceValidationWarningForMissingProtoDisplayNameEntry()
    {
        const int protoNumber = 21;

        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(gameDir, "data", "proto"));
        Directory.CreateDirectory(Path.Combine(gameDir, "data", "mes"));

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(protoNumber),
                Path.Combine(gameDir, "data", "proto", "00021 - MissingName.pro")
            );
            MessageFormat.WriteToFile(
                new MesFile { Entries = [new MessageEntry(999, "Different entry")] },
                Path.Combine(gameDir, "data", "mes", "description.mes")
            );

            var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(gameDir);
            var issue = workspace.Validation.Issues.Single(issue =>
                issue.AssetPath == "proto/00021 - MissingName.pro"
                && issue.Message.Contains("display-name entry", StringComparison.Ordinal)
            );

            await Assert.That(issue.Severity).IsEqualTo(EditorWorkspaceValidationSeverity.Warning);
            await Assert.That(issue.Message.Contains("lookup key 21", StringComparison.Ordinal)).IsTrue();
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadFromGameInstallAsync_UsesTranslatedProtoNameKeyForUapValidation()
    {
        const int protoNumber = 21;

        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(gameDir, "modules"));
        Directory.CreateDirectory(Path.Combine(gameDir, "data", "proto"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(gameDir, "data", "proto", "00021 - Uap.pro"));
            var descriptionMes = new MesFile { Entries = [new MessageEntry(1, "Translated display name")] };
            await WriteDatAsync(
                Path.Combine(gameDir, "modules", "Arcanum.PATCH0"),
                new Dictionary<string, byte[]>
                {
                    ["mes\\description.mes"] = MessageFormat.WriteToArray(in descriptionMes),
                }
            );

            var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(gameDir);

            await Assert.That(workspace.Validation.HasIssues).IsFalse();
            await Assert.That(workspace.Validation.Issues.Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_BuildsMessageAndProtoIndexes()
    {
        const int protoNumber = 1001;
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(Path.Combine(contentDir, "mob"));

        try
        {
            MessageFormat.WriteToFile(
                new MesFile { Entries = [new MessageEntry(10, "Hello index"), new MessageEntry(20, "World index")] },
                Path.Combine(contentDir, "mes", "game.mes")
            );
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            MobFormat.WriteToFile(MakePc(protoNumber), Path.Combine(contentDir, "mob", "test.mob"));
            SectorFormat.WriteToFile(
                MakeSector(MakePc(protoNumber), MakePc(protoNumber), MakePc(protoNumber + 1)),
                Path.Combine(contentDir, "maps", "map01", "sector.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);

            var messageAssets = workspace.Index.FindMessageAssets(10);
            var protoDefinition = workspace.Index.FindProtoDefinition(protoNumber);
            var protoReferences = workspace.Index.FindProtoReferences(protoNumber);

            await Assert.That(messageAssets.Count).IsEqualTo(1);
            await Assert.That(messageAssets[0].AssetPath).IsEqualTo("mes/game.mes");
            await Assert.That(protoDefinition).IsNotNull();
            await Assert.That(protoDefinition!.AssetPath).IsEqualTo("proto/001001 - Test.pro");
            await Assert.That(protoReferences.Count).IsEqualTo(2);

            var mobReference = protoReferences.Single(reference => reference.Asset.AssetPath == "mob/test.mob");
            var sectorReference = protoReferences.Single(reference =>
                reference.Asset.AssetPath == "maps/map01/sector.sec"
            );

            await Assert.That(mobReference.Count).IsEqualTo(1);
            await Assert.That(sectorReference.Count).IsEqualTo(2);
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_BuildsMapScriptAndArtIndexes()
    {
        const uint artId = 0x00112233u;
        const int scriptId = 777;
        const int protoNumber = 1001;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(Path.Combine(contentDir, "mob"));

        try
        {
            var mob = WithProperties(
                MakePc(protoNumber),
                MakeScriptProperty(scriptId),
                MakeArtProperty(ObjectField.ObjFAid, artId)
            );
            var proto = WithProperties(
                MakeProto(protoNumber),
                MakeScriptProperty(scriptId, scriptId),
                MakeArtProperty(ObjectField.ObjFLightAid, artId)
            );

            var tiles = new uint[4096];
            tiles[0] = artId;

            var sector = new Sector
            {
                Lights =
                [
                    new SectorLight
                    {
                        ObjHandle = -1,
                        TileLoc = 0,
                        OffsetX = 0,
                        OffsetY = 0,
                        Flags = 0,
                        ArtId = artId,
                        R = 0,
                        B = 0,
                        G = 0,
                        TintColor = 0,
                        Palette = 0,
                        Padding2C = 0,
                    },
                ],
                Tiles = tiles,
                HasRoofs = true,
                Roofs = [artId, artId, .. new uint[254]],
                SectorScript = MakeScript(scriptId),
                TileScripts =
                [
                    new TileScript
                    {
                        NodeFlags = 0,
                        TileId = 0,
                        ScriptFlags = 0,
                        ScriptCounters = 0,
                        ScriptNum = scriptId,
                    },
                ],
                TownmapInfo = 0,
                AptitudeAdjustment = 0,
                LightSchemeIdx = 0,
                SoundList = SectorSoundList.Default,
                BlockMask = new uint[128],
                Objects =
                [
                    WithProperties(
                        MakePc(protoNumber),
                        MakeScriptProperty(scriptId),
                        MakeArtProperty(ObjectField.ObjFCurrentAid, artId)
                    ),
                ],
            };

            ProtoFormat.WriteToFile(proto, Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            MobFormat.WriteToFile(mob, Path.Combine(contentDir, "mob", "test.mob"));
            SectorFormat.WriteToFile(sector, Path.Combine(contentDir, "maps", "map01", "sector.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);

            var mapAssets = workspace.Index.FindMapAssets("map01");
            var assetMap = workspace.Index.FindAssetMap("maps/map01/sector.sec");
            var scriptReferences = workspace.Index.FindScriptReferences(scriptId);
            var artReferences = workspace.Index.FindArtReferences(artId);

            await Assert.That(workspace.Index.MapNames.Count).IsEqualTo(1);
            await Assert.That(workspace.Index.MapNames[0]).IsEqualTo("map01");
            await Assert.That(mapAssets.Count).IsEqualTo(1);
            await Assert.That(mapAssets[0].AssetPath).IsEqualTo("maps/map01/sector.sec");
            await Assert.That(assetMap).IsEqualTo("map01");
            await Assert.That(scriptReferences.Count).IsEqualTo(3);
            await Assert.That(artReferences.Count).IsEqualTo(3);

            var mobScriptReference = scriptReferences.Single(reference => reference.Asset.AssetPath == "mob/test.mob");
            var protoScriptReference = scriptReferences.Single(reference =>
                reference.Asset.AssetPath == "proto/001001 - Test.pro"
            );
            var sectorScriptReference = scriptReferences.Single(reference =>
                reference.Asset.AssetPath == "maps/map01/sector.sec"
            );
            var mobArtReference = artReferences.Single(reference => reference.Asset.AssetPath == "mob/test.mob");
            var protoArtReference = artReferences.Single(reference =>
                reference.Asset.AssetPath == "proto/001001 - Test.pro"
            );
            var sectorArtReference = artReferences.Single(reference =>
                reference.Asset.AssetPath == "maps/map01/sector.sec"
            );

            await Assert.That(mobScriptReference.Count).IsEqualTo(1);
            await Assert.That(protoScriptReference.Count).IsEqualTo(2);
            await Assert.That(sectorScriptReference.Count).IsEqualTo(3);
            await Assert.That(mobArtReference.Count).IsEqualTo(1);
            await Assert.That(protoArtReference.Count).IsEqualTo(1);
            await Assert.That(sectorArtReference.Count).IsEqualTo(5);
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_IgnoresMalformedScriptReferenceProperties()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mob"));

        try
        {
            var malformedScriptProperty = new ObjectProperty
            {
                Field = ObjectField.ObjFScriptsIdx,
                RawBytes = [1, 0xFF, 0xFF, 0xFF, 0xFF, 0, 0, 0, 0, 0, 0, 0, 0],
            };
            var mob = WithProperties(MakePc(), malformedScriptProperty);
            MobFormat.WriteToFile(mob, Path.Combine(contentDir, "mob", "broken.mob"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);

            await Assert.That(workspace.GameData.Mobs.Count).IsEqualTo(1);
            await Assert.That(workspace.Index.FindScriptReferences(777).Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_LoadsOptionalSaveIntoWorkspace()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var saveDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(contentDir);
        Directory.CreateDirectory(saveDir);

        try
        {
            var mes = new MesFile { Entries = [new MessageEntry(20, "Workspace with save")] };
            MessageFormat.WriteToFile(in mes, Path.Combine(contentDir, "game.mes"));

            var save = MakeMinimalSave();
            SaveGameWriter.Save(save, saveDir, "slot0001");

            var workspace = await EditorWorkspaceLoader.LoadAsync(
                contentDir,
                new EditorWorkspaceLoadOptions { SaveFolder = saveDir, SaveSlotName = "slot0001" }
            );

            await Assert.That(workspace.HasSaveLoaded).IsTrue();
            await Assert.That(workspace.Save).IsNotNull();
            await Assert.That(workspace.Save!.Mobiles.ContainsKey("maps/map01/mobile/G_pc.mob")).IsTrue();

            var editor = workspace.CreateSaveEditor();
            await Assert.That(editor.GetCurrentSaveInfo().LeaderName).IsEqualTo("WorkspacePc");
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
            Directory.Delete(saveDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_WithPartialSaveSelection_ThrowsArgumentException()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(contentDir);

        try
        {
            await Assert
                .That(async () =>
                    await EditorWorkspaceLoader.LoadAsync(
                        contentDir,
                        new EditorWorkspaceLoadOptions { SaveFolder = contentDir }
                    )
                )
                .Throws<ArgumentException>();
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadFromGameInstallAsync_LoadsGameDataFromModuleDat()
    {
        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(gameDir);
        Directory.CreateDirectory(Path.Combine(gameDir, "modules"));

        try
        {
            var mes = new MesFile { Entries = [new MessageEntry(30, "Archive workspace")] };
            var archivePath = Path.Combine(gameDir, "modules", "Arcanum.dat");
            await WriteDatAsync(
                archivePath,
                new Dictionary<string, byte[]> { ["mes\\game.mes"] = MessageFormat.WriteToArray(in mes) }
            );

            var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(gameDir);
            var asset = workspace.Assets.Find("mes/game.mes");

            await Assert.That(workspace.GameDirectory).IsEqualTo(gameDir);
            await Assert.That(workspace.ContentDirectory).IsEqualTo(Path.Combine(gameDir, "data"));
            await Assert.That(workspace.GameData.Messages.Count).IsEqualTo(1);
            await Assert.That(workspace.GameData.Messages[0].Text).IsEqualTo("Archive workspace");
            await Assert.That(asset).IsNotNull();
            await Assert.That(asset!.SourceKind).IsEqualTo(EditorAssetSourceKind.DatArchive);
            await Assert.That(asset.SourcePath).IsEqualTo(archivePath);
            await Assert.That(asset.SourceEntryPath).IsEqualTo("mes/game.mes");
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadFromGameInstallAsync_LoadsDialogAndScriptAssetsFromArchive()
    {
        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(gameDir);
        Directory.CreateDirectory(Path.Combine(gameDir, "modules"));

        try
        {
            var archivePath = Path.Combine(gameDir, "modules", "Arcanum.dat");
            await WriteDatAsync(
                archivePath,
                new Dictionary<string, byte[]>
                {
                    ["scr\\00777Archive.scr"] = ScriptFormat.WriteToArray(MakeScriptFile("Archive script")),
                    ["dlg\\00123Archive.dlg"] = DialogFormat.WriteToArray(MakeDialogFile((1, "Archive dialog", 0))),
                }
            );

            var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(gameDir);
            var scriptAsset = workspace.Assets.Find("scr/00777Archive.scr");
            var dialogAsset = workspace.Assets.Find("dlg/00123Archive.dlg");

            await Assert.That(workspace.GameData.Scripts.Count).IsEqualTo(1);
            await Assert.That(workspace.GameData.Dialogs.Count).IsEqualTo(1);
            await Assert.That(scriptAsset).IsNotNull();
            await Assert.That(dialogAsset).IsNotNull();
            await Assert.That(scriptAsset!.SourceKind).IsEqualTo(EditorAssetSourceKind.DatArchive);
            await Assert.That(dialogAsset!.SourceKind).IsEqualTo(EditorAssetSourceKind.DatArchive);
            await Assert.That(scriptAsset.SourcePath).IsEqualTo(archivePath);
            await Assert.That(dialogAsset.SourcePath).IsEqualTo(archivePath);
            await Assert.That(workspace.Index.FindScriptDefinitions(777).Count).IsEqualTo(1);
            await Assert.That(workspace.Index.FindDialogDefinitions(123).Count).IsEqualTo(1);
            await Assert.That(workspace.Index.FindScriptDefinition(777)?.AssetPath).IsEqualTo("scr/00777Archive.scr");
            await Assert.That(workspace.Index.FindDialogDefinition(123)?.AssetPath).IsEqualTo("dlg/00123Archive.dlg");
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadFromGameInstallAsync_PatchArchiveOverridesModuleArchive()
    {
        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(gameDir);
        Directory.CreateDirectory(Path.Combine(gameDir, "modules"));

        try
        {
            var baseMes = new MesFile { Entries = [new MessageEntry(40, "Base text")] };
            var patchMes = new MesFile { Entries = [new MessageEntry(40, "Patched text")] };
            var patchArchivePath = Path.Combine(gameDir, "modules", "Arcanum.PATCH0");
            await WriteDatAsync(
                Path.Combine(gameDir, "modules", "Arcanum.dat"),
                new Dictionary<string, byte[]> { ["mes\\game.mes"] = MessageFormat.WriteToArray(in baseMes) }
            );
            await WriteDatAsync(
                patchArchivePath,
                new Dictionary<string, byte[]> { ["mes\\game.mes"] = MessageFormat.WriteToArray(in patchMes) }
            );

            var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(gameDir);
            var asset = workspace.Assets.Find("mes/game.mes");

            await Assert.That(workspace.GameData.Messages.Count).IsEqualTo(1);
            await Assert.That(workspace.GameData.Messages[0].Text).IsEqualTo("Patched text");
            await Assert.That(asset).IsNotNull();
            await Assert.That(asset!.SourceKind).IsEqualTo(EditorAssetSourceKind.DatArchive);
            await Assert.That(asset.SourcePath).IsEqualTo(patchArchivePath);
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadFromGameInstallAsync_LooseDataOverridesArchivedContent()
    {
        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(gameDir);
        Directory.CreateDirectory(Path.Combine(gameDir, "modules"));
        Directory.CreateDirectory(Path.Combine(gameDir, "data", "mes"));

        try
        {
            var archiveMes = new MesFile { Entries = [new MessageEntry(50, "Archive text")] };
            var looseMes = new MesFile { Entries = [new MessageEntry(50, "Loose override")] };
            var loosePath = Path.Combine(gameDir, "data", "mes", "game.mes");
            await WriteDatAsync(
                Path.Combine(gameDir, "modules", "Arcanum.dat"),
                new Dictionary<string, byte[]> { ["mes\\game.mes"] = MessageFormat.WriteToArray(in archiveMes) }
            );
            MessageFormat.WriteToFile(in looseMes, loosePath);

            var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(gameDir);
            var asset = workspace.Assets.Find("mes/game.mes");

            await Assert.That(workspace.GameData.Messages.Count).IsEqualTo(1);
            await Assert.That(workspace.GameData.Messages[0].Text).IsEqualTo("Loose override");
            await Assert.That(asset).IsNotNull();
            await Assert.That(asset!.SourceKind).IsEqualTo(EditorAssetSourceKind.LooseFile);
            await Assert.That(asset.SourcePath).IsEqualTo(loosePath);
            await Assert.That(asset.SourceEntryPath).IsNull();
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadFromGameInstallAsync_LoadsOptionalSaveIntoInstallBackedWorkspace()
    {
        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var saveDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(gameDir);
        Directory.CreateDirectory(Path.Combine(gameDir, "modules"));
        Directory.CreateDirectory(saveDir);

        try
        {
            var mes = new MesFile { Entries = [new MessageEntry(60, "Install save workspace")] };
            await WriteDatAsync(
                Path.Combine(gameDir, "modules", "Arcanum.dat"),
                new Dictionary<string, byte[]> { ["mes\\game.mes"] = MessageFormat.WriteToArray(in mes) }
            );

            var save = MakeMinimalSave();
            SaveGameWriter.Save(save, saveDir, "slot0001");

            var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(
                gameDir,
                new EditorWorkspaceLoadOptions { SaveFolder = saveDir, SaveSlotName = "slot0001" }
            );

            await Assert.That(workspace.HasSaveLoaded).IsTrue();
            await Assert.That(workspace.Save).IsNotNull();
            await Assert.That(workspace.GameData.Messages[0].Text).IsEqualTo("Install save workspace");

            var editor = workspace.CreateSaveEditor();
            await Assert.That(editor.GetCurrentSaveInfo().LeaderName).IsEqualTo("WorkspacePc");
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
            Directory.Delete(saveDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadFromGameInstallAsync_IgnoresUnsupportedDatCandidatesInGameRoot()
    {
        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(gameDir);
        Directory.CreateDirectory(Path.Combine(gameDir, "modules"));

        try
        {
            await File.WriteAllBytesAsync(Path.Combine(gameDir, "unins000.dat"), [0x00, 0x01, 0x02, 0x03]);

            var mes = new MesFile { Entries = [new MessageEntry(70, "Valid archive content")] };
            await WriteDatAsync(
                Path.Combine(gameDir, "modules", "Arcanum.dat"),
                new Dictionary<string, byte[]> { ["mes\\game.mes"] = MessageFormat.WriteToArray(in mes) }
            );

            var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(gameDir);

            await Assert.That(workspace.GameData.Messages.Count).IsEqualTo(1);
            await Assert.That(workspace.GameData.Messages[0].Text).IsEqualTo("Valid archive content");
            await Assert.That(workspace.LoadReport.SkippedArchiveCandidates.Count).IsEqualTo(1);
            await Assert
                .That(workspace.LoadReport.SkippedArchiveCandidates[0].Path)
                .IsEqualTo(Path.Combine(gameDir, "unins000.dat"));
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadFromGameInstallAsync_IgnoresMalformedWinningAssets()
    {
        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(gameDir);
        Directory.CreateDirectory(Path.Combine(gameDir, "modules"));

        try
        {
            var mes = new MesFile { Entries = [new MessageEntry(80, "Still loads good assets")] };
            await WriteDatAsync(
                Path.Combine(gameDir, "modules", "Arcanum.dat"),
                new Dictionary<string, byte[]>
                {
                    ["mes\\game.mes"] = MessageFormat.WriteToArray(in mes),
                    ["proto\\bad.pro"] = [0x01, 0x02, 0x03],
                }
            );

            var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(gameDir);

            await Assert.That(workspace.GameData.Messages.Count).IsEqualTo(1);
            await Assert.That(workspace.GameData.Protos.Count).IsEqualTo(0);
            await Assert.That(workspace.Assets.Count).IsEqualTo(1);
            await Assert.That(workspace.LoadReport.SkippedAssets.Count).IsEqualTo(1);
            await Assert.That(workspace.LoadReport.SkippedAssets[0].AssetPath).IsEqualTo("proto/bad.pro");
            await Assert.That(workspace.Assets.Find("proto/bad.pro")).IsNull();
            await Assert.That(workspace.Assets.Find("mes/game.mes")).IsNotNull();
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
        }
    }

    [Test]
    public async Task CreateSaveEditor_WithoutSave_ThrowsInvalidOperationException()
    {
        var workspace = new EditorWorkspace
        {
            ContentDirectory = "content",
            GameData = new ArcNET.GameData.GameDataStore(),
        };

        await Assert.That(() => workspace.CreateSaveEditor()).Throws<InvalidOperationException>();
    }
}
