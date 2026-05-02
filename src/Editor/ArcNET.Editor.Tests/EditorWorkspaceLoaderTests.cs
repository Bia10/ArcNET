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

    private static ArtPaletteEntry[] CreateArtPalette()
    {
        var palette = new ArtPaletteEntry[256];
        palette[1] = new ArtPaletteEntry(1, 2, 3);
        return palette;
    }

    private static ArtFile MakeArtFile(uint frameRate = 8, byte paletteIndex = 1) =>
        new()
        {
            Flags = ArtFlags.Static,
            FrameRate = frameRate,
            ActionFrame = 0,
            FrameCount = 1,
            DataSizes = new uint[8],
            PaletteData1 = new uint[8],
            PaletteData2 = new uint[8],
            PaletteIds = [1, 0, 0, 0],
            Palettes = [CreateArtPalette(), null, null, null],
            Frames =
            [
                [new ArtFrame { Header = new ArtFrameHeader(1u, 1u, 1u, 0, 0, 0, 0), Pixels = [paletteIndex] }],
            ],
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

    private static Sector MakeSector(params MobData[] objects) => MakeSector(0, -1, -1, objects);

    private static Sector MakeSector(
        int lightSchemeIdx,
        int musicSchemeIdx,
        int ambientSchemeIdx,
        params MobData[] objects
    ) =>
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
            LightSchemeIdx = lightSchemeIdx,
            SoundList = new SectorSoundList
            {
                Flags = 0,
                MusicSchemeIdx = musicSchemeIdx,
                AmbientSchemeIdx = ambientSchemeIdx,
            },
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

    private static long PackTileLocation(int x, int y) => ((long)y << 32) | (uint)x;

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
    public async Task LoadAsync_LoadsArtAssetsAndBuildsWorkspacePreview()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "art", "critters"));

        try
        {
            var artPath = Path.Combine(contentDir, "art", "critters", "barbarian.art");
            var artFile = MakeArtFile(frameRate: 12);
            ArtFormat.WriteToFile(in artFile, artPath);

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var asset = workspace.Assets.Find("art/critters/barbarian.art");
            var art = workspace.FindArt("art/critters/barbarian.art");
            var preview = workspace.CreateArtPreview("art/critters/barbarian.art");

            await Assert.That(workspace.GameData.Arts.Count).IsEqualTo(1);
            await Assert.That(workspace.Assets.FindByFormat(FileFormat.Art).Count).IsEqualTo(1);
            await Assert.That(asset).IsNotNull();
            await Assert.That(asset!.SourceKind).IsEqualTo(EditorAssetSourceKind.LooseFile);
            await Assert.That(asset.SourcePath).IsEqualTo(artPath);
            await Assert.That(art).IsNotNull();
            await Assert.That(art!.FrameRate).IsEqualTo(12u);
            await Assert.That(preview.FrameRate).IsEqualTo(12u);
            await Assert.That(preview.Frames.Count).IsEqualTo(1);
            await Assert.That(preview.Frames[0].PixelData.SequenceEqual(new byte[] { 3, 2, 1, 255 })).IsTrue();
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_LoadsAdditionalWorkspaceFormats_AndExposesDirectLookups()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(Path.Combine(contentDir, "rules"));
        Directory.CreateDirectory(Path.Combine(contentDir, "walk"));

        try
        {
            var message = new MesFile { Entries = [new MessageEntry(10, "Workspace message")] };
            var proto = MakeProto(1001);
            var jumpFile = new JmpFile
            {
                Jumps =
                [
                    new JumpEntry
                    {
                        Flags = 0,
                        SourceLoc = PackTileLocation(2, 4),
                        DestinationMapId = 42,
                        DestinationLoc = PackTileLocation(12, 18),
                    },
                ],
            };
            var mapProperties = new MapProperties
            {
                ArtId = 77,
                Unused = 0,
                LimitX = 64,
                LimitY = 64,
            };
            var terrain = new TerrainData
            {
                Version = 1.2f,
                BaseTerrainType = TerrainType.Grasslands,
                Width = 2,
                Height = 2,
                Compressed = false,
                Tiles = [1, 2, 3, 4],
            };
            var facadeWalk = new FacadeWalk
            {
                Header = new FacWalkHeader(16, 1, 0, 8, 8),
                Entries = [new FacWalkEntry(5, 6, true)],
            };

            MessageFormat.WriteToFile(in message, Path.Combine(contentDir, "mes", "game.mes"));
            ProtoFormat.WriteToFile(proto, Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            JmpFormat.WriteToFile(jumpFile, Path.Combine(contentDir, "maps", "map01", "map01.jmp"));
            MapPropertiesFormat.WriteToFile(in mapProperties, Path.Combine(contentDir, "maps", "map01", "map.prp"));
            TerrainFormat.WriteToFile(in terrain, Path.Combine(contentDir, "rules", "terrain.tdf"));
            FacWalkFormat.WriteToFile(in facadeWalk, Path.Combine(contentDir, "walk", "facwalk.test"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var jumpDetail = workspace.Index.FindJumpDetail("maps/map01/map01.jmp");
            var mapPropertiesDetail = workspace.Index.FindMapPropertiesDetail("maps/map01/map.prp");
            var terrainDetail = workspace.Index.FindTerrainDetail("rules/terrain.tdf");
            var facadeWalkDetail = workspace.Index.FindFacadeWalkDetail("walk/facwalk.test");

            await Assert.That(workspace.Assets.FindByFormat(FileFormat.Message).Count).IsEqualTo(1);
            await Assert.That(workspace.Assets.FindByFormat(FileFormat.Proto).Count).IsEqualTo(1);
            await Assert.That(workspace.Assets.FindByFormat(FileFormat.Jmp).Count).IsEqualTo(1);
            await Assert.That(workspace.Assets.FindByFormat(FileFormat.MapProperties).Count).IsEqualTo(1);
            await Assert.That(workspace.Assets.FindByFormat(FileFormat.Terrain).Count).IsEqualTo(1);
            await Assert.That(workspace.Assets.FindByFormat(FileFormat.FacadeWalk).Count).IsEqualTo(1);
            await Assert.That(workspace.FindMessageFile("mes/game.mes")).IsNotNull();
            await Assert.That(workspace.FindProto("proto/001001 - Test.pro")).IsNotNull();
            await Assert.That(workspace.FindJumpFile("maps/map01/map01.jmp")?.Jumps[0].DestinationMapId).IsEqualTo(42);
            await Assert.That(workspace.FindMapProperties("maps/map01/map.prp")?.ArtId).IsEqualTo(77);
            await Assert.That(workspace.FindTerrain("rules/terrain.tdf")?.Tiles.Length).IsEqualTo(4);
            await Assert.That(workspace.FindFacadeWalk("walk/facwalk.test")?.Entries.Length).IsEqualTo(1);
            await Assert.That(jumpDetail).IsNotNull();
            await Assert.That(jumpDetail!.JumpCount).IsEqualTo(1);
            await Assert.That(jumpDetail.DestinationMapIds[0]).IsEqualTo(42);
            await Assert.That(mapPropertiesDetail).IsNotNull();
            await Assert.That(mapPropertiesDetail!.LimitX).IsEqualTo(64UL);
            await Assert.That(terrainDetail).IsNotNull();
            await Assert.That(terrainDetail!.DistinctTileCount).IsEqualTo(4);
            await Assert.That(facadeWalkDetail).IsNotNull();
            await Assert.That(facadeWalkDetail!.WalkableEntryCount).IsEqualTo(1);
            await Assert.That(workspace.Index.SearchJumpDetails("42").Count).IsEqualTo(1);
            await Assert.That(workspace.Index.SearchMapPropertiesDetails("77").Count).IsEqualTo(1);
            await Assert.That(workspace.Index.SearchTerrainDetails("grass").Count).IsEqualTo(1);
            await Assert.That(workspace.Index.SearchFacadeWalkDetails("facwalk").Count).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_IndexesArtDetails_ForBrowserAndPreviewWorkflows()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "art", "critters"));

        try
        {
            var artPath = Path.Combine(contentDir, "art", "critters", "barbarian.art");
            var artFile = MakeArtFile(frameRate: 12);
            ArtFormat.WriteToFile(in artFile, artPath);

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var detail = workspace.Index.FindArtDetail("art/critters/barbarian.art");
            var searchResults = workspace.Index.SearchArtDetails("barbarian");
            var preview = workspace.CreateArtPreview("art/critters/barbarian.art");

            await Assert.That(detail).IsNotNull();
            await Assert.That(searchResults.Count).IsEqualTo(1);
            await Assert.That(searchResults[0].Asset.AssetPath).IsEqualTo("art/critters/barbarian.art");
            await Assert.That(detail!.Format).IsEqualTo(FileFormat.Art);
            await Assert.That(detail.Flags).IsEqualTo(artFile.Flags);
            await Assert.That(detail.FrameRate).IsEqualTo(12u);
            await Assert.That(detail.ActionFrame).IsEqualTo(artFile.ActionFrame);
            await Assert.That(detail.RotationCount).IsEqualTo(1);
            await Assert.That(detail.FramesPerRotation).IsEqualTo(1);
            await Assert.That(detail.PaletteCount).IsEqualTo(1);
            await Assert.That(detail.MaxFrameWidth).IsEqualTo(1);
            await Assert.That(detail.MaxFrameHeight).IsEqualTo(1);
            await Assert.That(detail.IsAnimated).IsTrue();
            await Assert.That(preview.Frames[0].Width).IsEqualTo(detail.MaxFrameWidth);
            await Assert.That(preview.Frames[0].Height).IsEqualTo(detail.MaxFrameHeight);
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_CreateArtResolver_BindsArtIdToLoadedArtAndBuildsScenePreview()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var artId = new ArtId(0x01020304u);
        var protoId = MakeProtoId(protoNumber);
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid());

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(Path.Combine(contentDir, "art", "critters"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            ArtFormat.WriteToFile(
                MakeArtFile(frameRate: 12),
                Path.Combine(contentDir, "art", "critters", "barbarian.art")
            );
            SectorFormat.WriteToFile(
                MakeSector(
                    new CharacterBuilder(ObjectType.Npc, objectId, protoId)
                        .WithHitPoints(80)
                        .WithProperty(MakeArtProperty(ObjectField.ObjFCurrentAid, artId.Value))
                        .Build()
                ),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var artResolver = workspace.CreateArtResolver();
            artResolver.Bind(artId, "art/critters/barbarian.art");

            var preview = workspace.CreateMapScenePreview("map01", artResolver);

            await Assert.That(artResolver.BindingCount).IsEqualTo(1);
            await Assert.That(artResolver.FindAssetPath(artId)).IsEqualTo("art/critters/barbarian.art");
            await Assert.That(artResolver.FindArt(artId)).IsNotNull();
            await Assert.That(preview.Sectors.Count).IsEqualTo(1);
            await Assert.That(preview.Sectors[0].Objects.Count).IsEqualTo(1);
            await Assert.That(preview.Sectors[0].Objects[0].CurrentArtId).IsEqualTo(artId);
            await Assert.That(preview.Sectors[0].Objects[0].SpriteBounds).IsNotNull();
            await Assert.That(preview.Sectors[0].Objects[0].SpriteBounds!.MaxFrameWidth).IsEqualTo(1);
            await Assert.That(preview.Sectors[0].Objects[0].SpriteBounds!.MaxFrameHeight).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_CreateArtResolver_WithConservativeStrategy_BindsUnambiguousNumericArtAssets()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var artId = new ArtId(200u);
        var protoId = MakeProtoId(protoNumber);
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid());

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(Path.Combine(contentDir, "art"));

        try
        {
            ProtoFormat.WriteToFile(
                WithProperties(MakeProto(protoNumber), MakeArtProperty(ObjectField.ObjFCurrentAid, artId.Value)),
                Path.Combine(contentDir, "proto", "001001 - Test.pro")
            );
            ArtFormat.WriteToFile(MakeArtFile(frameRate: 12), Path.Combine(contentDir, "art", "200.art"));
            SectorFormat.WriteToFile(
                MakeSector(
                    new CharacterBuilder(ObjectType.Npc, objectId, protoId)
                        .WithHitPoints(80)
                        .WithProperty(MakeArtProperty(ObjectField.ObjFCurrentAid, artId.Value))
                        .Build()
                ),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var artResolver = workspace.CreateArtResolver(EditorArtResolverBindingStrategy.Conservative);

            var preview = workspace.CreateMapScenePreview("map01", artResolver);
            var paletteEntry = workspace.FindObjectPaletteEntry(
                protoNumber,
                EditorArtResolverBindingStrategy.Conservative
            );

            await Assert.That(artResolver.BindingCount).IsEqualTo(1);
            await Assert.That(artResolver.FindAssetPath(artId)).IsEqualTo("art/200.art");
            await Assert.That(preview.Sectors[0].Objects[0].SpriteBounds).IsNotNull();
            await Assert.That(paletteEntry).IsNotNull();
            await Assert.That(paletteEntry!.ArtAssetPath).IsEqualTo("art/200.art");
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_CreateArtResolver_WithConservativeStrategy_SkipsAmbiguousNumericArtAssets()
    {
        const int protoNumber = 1001;
        var artId = new ArtId(200u);
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "art", "a"));
        Directory.CreateDirectory(Path.Combine(contentDir, "art", "b"));

        try
        {
            ProtoFormat.WriteToFile(
                WithProperties(MakeProto(protoNumber), MakeArtProperty(ObjectField.ObjFCurrentAid, artId.Value)),
                Path.Combine(contentDir, "proto", "001001 - Test.pro")
            );
            ArtFormat.WriteToFile(MakeArtFile(frameRate: 12), Path.Combine(contentDir, "art", "a", "200.art"));
            ArtFormat.WriteToFile(MakeArtFile(frameRate: 12), Path.Combine(contentDir, "art", "b", "200.art"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var artResolver = workspace.CreateArtResolver(EditorArtResolverBindingStrategy.Conservative);

            var paletteEntry = workspace.FindObjectPaletteEntry(
                protoNumber,
                EditorArtResolverBindingStrategy.Conservative
            );

            await Assert.That(artResolver.BindingCount).IsEqualTo(0);
            await Assert.That(artResolver.FindAssetPath(artId)).IsNull();
            await Assert.That(paletteEntry).IsNotNull();
            await Assert.That(paletteEntry!.ArtAssetPath).IsNull();
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_GetTerrainPalette_ReturnsEntriesDerivedFromMapPropertiesAndSupportsConservativeArtBinding()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(Path.Combine(contentDir, "art", "ground"));

        try
        {
            MapProperties mapProperties = new()
            {
                ArtId = 200,
                Unused = 0,
                LimitX = 2,
                LimitY = 2,
            };
            MapPropertiesFormat.WriteToFile(in mapProperties, Path.Combine(contentDir, "maps", "map01", "map.prp"));
            ArtFormat.WriteToFile(MakeArtFile(frameRate: 12), Path.Combine(contentDir, "art", "ground", "200.art"));
            ArtFormat.WriteToFile(MakeArtFile(frameRate: 12), Path.Combine(contentDir, "art", "ground", "201.art"));
            ArtFormat.WriteToFile(MakeArtFile(frameRate: 12), Path.Combine(contentDir, "art", "ground", "202.art"));
            ArtFormat.WriteToFile(MakeArtFile(frameRate: 12), Path.Combine(contentDir, "art", "ground", "203.art"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);

            var palette = workspace.GetTerrainPaletteForMap("map01", EditorArtResolverBindingStrategy.Conservative);

            await Assert.That(palette.Count).IsEqualTo(4);
            await Assert.That(palette[0].PaletteX).IsEqualTo(0UL);
            await Assert.That(palette[0].PaletteY).IsEqualTo(0UL);
            await Assert.That(palette[0].ArtId).IsEqualTo(new ArtId(200u));
            await Assert.That(palette[0].ArtAssetPath).IsEqualTo("art/ground/200.art");
            await Assert.That(palette[3].PaletteX).IsEqualTo(1UL);
            await Assert.That(palette[3].PaletteY).IsEqualTo(1UL);
            await Assert.That(palette[3].ArtId).IsEqualTo(new ArtId(203u));
            await Assert.That(palette[3].ArtAssetPath).IsEqualTo("art/ground/203.art");
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
            var scriptDependencies = workspace.Index.FindAssetDependencySummary("scr/00777Alpha.scr");
            var dialogDependencies = workspace.Index.FindAssetDependencySummary("dlg/00123Alpha.dlg");

            await Assert.That(workspace.GameData.Scripts.Count).IsEqualTo(2);
            await Assert.That(workspace.GameData.Dialogs.Count).IsEqualTo(2);
            await Assert.That(scriptAsset).IsNotNull();
            await Assert.That(dialogAsset).IsNotNull();
            await Assert.That(scriptDependencies).IsNotNull();
            await Assert.That(dialogDependencies).IsNotNull();
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
            await Assert.That(scriptDependencies!.DefinedScriptId).IsEqualTo(777);
            await Assert.That(scriptDependencies.DefinedProtoNumber.HasValue).IsFalse();
            await Assert.That(scriptDependencies.DefinedDialogId.HasValue).IsFalse();
            await Assert.That(scriptDependencies.HasDependencies).IsFalse();
            await Assert.That(scriptDependencies.HasIncomingReferences).IsFalse();
            await Assert.That(scriptDependencies.HasRelationships).IsFalse();
            await Assert.That(scriptDependencies.ProtoReferences.Count).IsEqualTo(0);
            await Assert.That(scriptDependencies.ScriptReferences.Count).IsEqualTo(0);
            await Assert.That(scriptDependencies.ArtReferences.Count).IsEqualTo(0);
            await Assert.That(scriptDependencies.IncomingProtoReferences.Count).IsEqualTo(0);
            await Assert.That(scriptDependencies.IncomingScriptReferences.Count).IsEqualTo(0);
            await Assert.That(dialogDependencies!.DefinedDialogId).IsEqualTo(123);
            await Assert.That(dialogDependencies.DefinedProtoNumber.HasValue).IsFalse();
            await Assert.That(dialogDependencies.DefinedScriptId.HasValue).IsFalse();
            await Assert.That(dialogDependencies.HasDependencies).IsFalse();
            await Assert.That(dialogDependencies.HasIncomingReferences).IsFalse();
            await Assert.That(dialogDependencies.HasRelationships).IsFalse();
            await Assert.That(dialogDependencies.ProtoReferences.Count).IsEqualTo(0);
            await Assert.That(dialogDependencies.ScriptReferences.Count).IsEqualTo(0);
            await Assert.That(dialogDependencies.ArtReferences.Count).IsEqualTo(0);
            await Assert.That(dialogDependencies.IncomingProtoReferences.Count).IsEqualTo(0);
            await Assert.That(dialogDependencies.IncomingScriptReferences.Count).IsEqualTo(0);
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
            await Assert.That(dialogDetails[0].Nodes.Count).IsEqualTo(5);
            await Assert
                .That(
                    dialogDetails[0].Nodes.Select(static node => node.EntryNumber).SequenceEqual([10, 20, 30, 35, 40])
                )
                .IsTrue();
            await Assert.That(dialogDetails[0].RootEntryNumbers.SequenceEqual([10, 35, 40])).IsTrue();
            await Assert.That(dialogDetails[0].MissingResponseTargetNumbers.SequenceEqual([999])).IsTrue();
            await Assert.That(dialogDetails[0].HasMissingResponseTargets).IsTrue();

            var helloNode = dialogDetails[0].Nodes.Single(static node => node.EntryNumber == 10);
            var askNode = dialogDetails[0].Nodes.Single(static node => node.EntryNumber == 20);
            var exitNode = dialogDetails[0].Nodes.Single(static node => node.EntryNumber == 30);
            var specialNode = dialogDetails[0].Nodes.Single(static node => node.EntryNumber == 35);
            var orphanNode = dialogDetails[0].Nodes.Single(static node => node.EntryNumber == 40);

            await Assert.That(helloNode.Kind).IsEqualTo(EditorDialogNodeKind.NpcReply);
            await Assert.That(helloNode.IsRoot).IsTrue();
            await Assert.That(helloNode.HasTransition).IsTrue();
            await Assert.That(helloNode.IsTerminal).IsFalse();
            await Assert.That(helloNode.ResponseTargetNumber).IsEqualTo(20);
            await Assert.That(helloNode.HasMissingResponseTarget).IsFalse();

            await Assert.That(askNode.Kind).IsEqualTo(EditorDialogNodeKind.PcOption);
            await Assert.That(askNode.IsRoot).IsFalse();
            await Assert.That(askNode.HasTransition).IsTrue();
            await Assert.That(askNode.IsTerminal).IsFalse();
            await Assert.That(askNode.ResponseTargetNumber).IsEqualTo(30);
            await Assert.That(askNode.HasMissingResponseTarget).IsFalse();

            await Assert.That(exitNode.Kind).IsEqualTo(EditorDialogNodeKind.Control);
            await Assert.That(exitNode.IsRoot).IsFalse();
            await Assert.That(exitNode.HasTransition).IsFalse();
            await Assert.That(exitNode.IsTerminal).IsTrue();
            await Assert.That(exitNode.HasMissingResponseTarget).IsFalse();

            await Assert.That(specialNode.Kind).IsEqualTo(EditorDialogNodeKind.NpcReply);
            await Assert.That(specialNode.IsRoot).IsTrue();
            await Assert.That(specialNode.HasTransition).IsFalse();
            await Assert.That(specialNode.IsTerminal).IsFalse();
            await Assert.That(specialNode.ResponseTargetNumber).IsEqualTo(-1);
            await Assert.That(specialNode.HasMissingResponseTarget).IsFalse();

            await Assert.That(orphanNode.Kind).IsEqualTo(EditorDialogNodeKind.NpcReply);
            await Assert.That(orphanNode.IsRoot).IsTrue();
            await Assert.That(orphanNode.HasTransition).IsTrue();
            await Assert.That(orphanNode.IsTerminal).IsFalse();
            await Assert.That(orphanNode.ResponseTargetNumber).IsEqualTo(999);
            await Assert.That(orphanNode.HasMissingResponseTarget).IsTrue();
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_SearchesAssetsAndIndexedEditorSemantics()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "art", "critters"));
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "StillwaterDocks"));

        try
        {
            ArtFormat.WriteToFile(MakeArtFile(), Path.Combine(contentDir, "art", "critters", "stillwater_guard.art"));
            ScriptFormat.WriteToFile(
                MakeScriptFile("Stillwater dock guard", conditionCount: 2),
                Path.Combine(contentDir, "scr", "00077Stillwater.scr")
            );
            DialogFormat.WriteToFile(
                MakeDialogFile((1, "Welcome to Stillwater", 0)),
                Path.Combine(contentDir, "dlg", "00123Stillwater.dlg")
            );
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "StillwaterDocks", "sector_guard_post.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);

            var allStillwaterAssets = workspace.Assets.Search("  STILLWATER ");
            var artAssets = workspace.Assets.Search("guard", FileFormat.Art);
            var mapNames = workspace.Index.SearchMapNames("still");
            var sectors = workspace.Index.SearchSectors("docks");
            var scriptDetails = workspace.Index.SearchScriptDetails("dock guard");
            var scriptDetailsById = workspace.Index.SearchScriptDetails("77");
            var dialogDetails = workspace.Index.SearchDialogDetails("welcome");
            var dialogDetailsById = workspace.Index.SearchDialogDetails("123");

            await Assert
                .That(
                    allStillwaterAssets
                        .Select(static asset => asset.AssetPath)
                        .SequenceEqual([
                            "art/critters/stillwater_guard.art",
                            "dlg/00123Stillwater.dlg",
                            "maps/StillwaterDocks/sector_guard_post.sec",
                            "scr/00077Stillwater.scr",
                        ])
                )
                .IsTrue();
            await Assert.That(artAssets.Count).IsEqualTo(1);
            await Assert.That(artAssets[0].AssetPath).IsEqualTo("art/critters/stillwater_guard.art");
            await Assert.That(mapNames.SequenceEqual(["StillwaterDocks"])).IsTrue();
            await Assert.That(sectors.Count).IsEqualTo(1);
            await Assert.That(sectors[0].MapName).IsEqualTo("StillwaterDocks");
            await Assert.That(sectors[0].Asset.AssetPath).IsEqualTo("maps/StillwaterDocks/sector_guard_post.sec");
            await Assert.That(scriptDetails.Count).IsEqualTo(1);
            await Assert.That(scriptDetails[0].ScriptId).IsEqualTo(77);
            await Assert.That(scriptDetails[0].Description).IsEqualTo("Stillwater dock guard");
            await Assert.That(scriptDetailsById.Count).IsEqualTo(1);
            await Assert.That(scriptDetailsById[0].Asset.AssetPath).IsEqualTo("scr/00077Stillwater.scr");
            await Assert.That(dialogDetails.Count).IsEqualTo(1);
            await Assert.That(dialogDetails[0].DialogId).IsEqualTo(123);
            await Assert.That(dialogDetails[0].Asset.AssetPath).IsEqualTo("dlg/00123Stillwater.dlg");
            await Assert.That(dialogDetailsById.Count).IsEqualTo(1);
            await Assert.That(dialogDetailsById[0].Nodes[0].Text).IsEqualTo("Welcome to Stillwater");
            await Assert.That(workspace.Index.SearchMapNames("tarant").Count).IsEqualTo(0);
            await Assert.That(workspace.Index.SearchScriptDetails("necromancer").Count).IsEqualTo(0);
            await Assert.That(workspace.Index.SearchDialogDetails("virgil").Count).IsEqualTo(0);
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
                new DlgFile
                {
                    Entries =
                    [
                        new DialogEntry
                        {
                            Num = 1,
                            Text = "Hello",
                            GenderField = string.Empty,
                            Iq = 0,
                            Conditions = string.Empty,
                            ResponseVal = 999,
                            Actions = string.Empty,
                        },
                        new DialogEntry
                        {
                            Num = 2,
                            Text = "Broken IQ",
                            GenderField = string.Empty,
                            Iq = -1,
                            Conditions = string.Empty,
                            ResponseVal = 0,
                            Actions = string.Empty,
                        },
                    ],
                },
                Path.Combine(contentDir, "dlg", "00001Broken.dlg")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var issues = workspace.Validation.Issues;

            await Assert.That(workspace.Validation.HasIssues).IsTrue();
            await Assert.That(workspace.Validation.HasErrors).IsTrue();
            await Assert.That(issues.Count).IsEqualTo(4);

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
            var dialogIqIssue = issues.Single(issue =>
                issue.Severity == EditorWorkspaceValidationSeverity.Error
                && issue.AssetPath == "dlg/00001Broken.dlg"
                && issue.Message.Contains("Negative IQ requirement", StringComparison.Ordinal)
            );

            await Assert.That(protoIssue.Message.Contains("proto 1001", StringComparison.Ordinal)).IsTrue();
            await Assert.That(scriptIssue.Message.Contains("script 777", StringComparison.Ordinal)).IsTrue();
            await Assert.That(dialogIssue.Message.Contains("999", StringComparison.Ordinal)).IsTrue();
            await Assert.That(dialogIqIssue.Message.Contains("entry 2", StringComparison.Ordinal)).IsTrue();
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
            await Assert.That(issue.Message.Contains("Script 2", StringComparison.Ordinal)).IsTrue();
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
    public async Task LoadAsync_IndexesMessageDetails_ForBrowserWorkflows()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));

        try
        {
            MessageFormat.WriteToFile(
                new MesFile { Entries = [new MessageEntry(10, "Hello index"), new MessageEntry(20, "World browser")] },
                Path.Combine(contentDir, "mes", "game.mes")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var detail = workspace.FindMessageDetail("mes/game.mes");
            var textSearch = workspace.SearchMessageDetails("browser");
            var indexSearch = workspace.SearchMessageDetails("20");

            await Assert.That(detail).IsNotNull();
            await Assert.That(detail!.Format).IsEqualTo(FileFormat.Message);
            await Assert.That(detail.EntryCount).IsEqualTo(2);
            await Assert.That(detail.MinEntryIndex).IsEqualTo(10);
            await Assert.That(detail.MaxEntryIndex).IsEqualTo(20);
            await Assert.That(detail.Entries[1].Text).IsEqualTo("World browser");
            await Assert.That(textSearch.Count).IsEqualTo(1);
            await Assert.That(textSearch[0].Asset.AssetPath).IsEqualTo("mes/game.mes");
            await Assert.That(indexSearch.Count).IsEqualTo(1);
            await Assert.That(indexSearch[0].Asset.AssetPath).IsEqualTo("mes/game.mes");
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_BuildsObjectPaletteEntries_ForProtoBrowsingWorkflows()
    {
        const int protoNumber = 1001;
        const uint artId = 0x00112233u;
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));

        try
        {
            MessageFormat.WriteToFile(
                new MesFile
                {
                    Entries =
                    [
                        new MessageEntry(protoNumber, "Palette proto"),
                        new MessageEntry(10, "Palette description"),
                    ],
                },
                Path.Combine(contentDir, "mes", "description.mes")
            );

            var proto = WithProperties(
                MakeProto(protoNumber),
                ObjectPropertyFactory.ForInt32(ObjectField.ObjFName, 20),
                ObjectPropertyFactory.ForInt32(ObjectField.ObjFDescription, 10),
                MakeArtProperty(ObjectField.ObjFCurrentAid, artId)
            );
            ProtoFormat.WriteToFile(proto, Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var entry = workspace.FindObjectPaletteEntry(protoNumber);
            var displayNameSearch = workspace.SearchObjectPalette("Palette proto");
            var numberSearch = workspace.SearchObjectPalette("1001");

            await Assert.That(entry).IsNotNull();
            await Assert.That(entry!.Asset.AssetPath).IsEqualTo("proto/001001 - Test.pro");
            await Assert.That(entry.ObjectType).IsEqualTo(ObjectType.Pc);
            await Assert.That(entry.DisplayName).IsEqualTo("Palette proto");
            await Assert.That(entry.NameMessageIndex).IsEqualTo(20);
            await Assert.That(entry.DescriptionMessageIndex).IsEqualTo(10);
            await Assert.That(entry.Description).IsEqualTo("Palette description");
            await Assert.That(entry.CurrentArtId).IsEqualTo(new ArtId(artId));
            await Assert.That(entry.CreateStampRequest().ProtoNumber).IsEqualTo(protoNumber);
            await Assert.That(entry.CreateReplaceRequest().ProtoNumber).IsEqualTo(protoNumber);
            var placementRequest = entry.CreatePlacementRequest(
                deltaTileX: 1,
                deltaTileY: 2,
                rotation: 0.5f,
                rotationPitch: 1.25f
            );
            await Assert.That(placementRequest.ProtoNumber).IsEqualTo(protoNumber);
            await Assert.That(placementRequest.DeltaTileX).IsEqualTo(1);
            await Assert.That(placementRequest.DeltaTileY).IsEqualTo(2);
            await Assert.That(placementRequest.Rotation).IsEqualTo(0.5f);
            await Assert.That(placementRequest.RotationPitch).IsEqualTo(1.25f);
            await Assert.That(placementRequest.AlignToTileGrid).IsTrue();
            var placementPreset = entry.CreatePlacementPreset(
                "palette.proto.1001",
                description: "Preset description",
                deltaTileX: 1,
                deltaTileY: 2,
                rotation: 0.5f,
                rotationPitch: 1.25f
            );
            await Assert.That(placementPreset.PresetId).IsEqualTo("palette.proto.1001");
            await Assert.That(placementPreset.Name).IsEqualTo("Palette proto");
            await Assert.That(placementPreset.Description).IsEqualTo("Preset description");
            await Assert.That(placementPreset.Entries.Count).IsEqualTo(1);
            await Assert.That(placementPreset.Entries[0].ProtoNumber).IsEqualTo(protoNumber);
            await Assert.That(placementPreset.Entries[0].AlignToTileGrid).IsTrue();
            var freePlacementRequest = entry.CreatePlacementRequest(alignToTileGrid: false);
            await Assert.That(freePlacementRequest.AlignToTileGrid).IsFalse();
            await Assert.That(displayNameSearch.Count).IsEqualTo(1);
            await Assert.That(displayNameSearch[0].ProtoNumber).IsEqualTo(protoNumber);
            await Assert.That(numberSearch.Count).IsEqualTo(1);
            await Assert.That(numberSearch[0].ProtoNumber).IsEqualTo(protoNumber);
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_EnrichesObjectPaletteEntries_WithGroupingAndArtBindings()
    {
        const int protoNumber = 1001;
        var artId = new ArtId(0x00112233u);
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto", "items"));
        Directory.CreateDirectory(Path.Combine(contentDir, "art", "critters"));

        try
        {
            MessageFormat.WriteToFile(
                new MesFile { Entries = [new MessageEntry(protoNumber, "Grouped palette proto")] },
                Path.Combine(contentDir, "mes", "description.mes")
            );
            ProtoFormat.WriteToFile(
                WithProperties(MakeProto(protoNumber), MakeArtProperty(ObjectField.ObjFCurrentAid, artId.Value)),
                Path.Combine(contentDir, "proto", "items", "001001 - Test.pro")
            );
            ArtFormat.WriteToFile(
                MakeArtFile(frameRate: 12),
                Path.Combine(contentDir, "art", "critters", "barbarian.art")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var artResolver = workspace.CreateArtResolver();
            artResolver.Bind(artId, "art/critters/barbarian.art");
            var previewOptions = new EditorArtPreviewOptions
            {
                PaletteSlot = 0,
                PixelFormat = EditorArtPreviewPixelFormat.Rgba32,
            };

            var entry = workspace.FindObjectPaletteEntry(protoNumber, artResolver);
            var previewEntry = workspace.FindObjectPaletteEntry(protoNumber, artResolver, previewOptions);
            var groupSearch = workspace.SearchObjectPalette("items", artResolver);
            var artSearch = workspace.SearchObjectPalette("barbarian.art", artResolver);
            var previewSearch = workspace.SearchObjectPalette("Grouped palette proto", artResolver, previewOptions);

            await Assert.That(entry).IsNotNull();
            await Assert.That(entry!.Category).IsEqualTo(nameof(ObjectType.Pc));
            await Assert.That(entry.PaletteGroup).IsEqualTo("items");
            await Assert.That(entry.CurrentArtId).IsEqualTo(artId);
            await Assert.That(entry.HasArtBinding).IsTrue();
            await Assert.That(entry.ArtAssetPath).IsEqualTo("art/critters/barbarian.art");
            await Assert.That(entry.DisplayName).IsEqualTo("Grouped palette proto");
            await Assert.That(entry.ArtDetail).IsNotNull();
            await Assert.That(entry.ArtPreview).IsNull();
            await Assert.That(previewEntry).IsNotNull();
            await Assert.That(previewEntry!.ArtDetail).IsNotNull();
            await Assert.That(previewEntry.ArtPreview).IsNotNull();
            await Assert.That(previewEntry.ArtDetail!.Asset.AssetPath).IsEqualTo("art/critters/barbarian.art");
            await Assert.That(previewEntry.ArtDetail.MaxFrameWidth).IsEqualTo(1);
            await Assert.That(previewEntry.ArtPreview!.Frames.Count).IsEqualTo(1);
            await Assert.That(previewEntry.ArtPreview.PixelFormat).IsEqualTo(EditorArtPreviewPixelFormat.Rgba32);
            await Assert.That(groupSearch.Count).IsEqualTo(1);
            await Assert.That(groupSearch[0].ProtoNumber).IsEqualTo(protoNumber);
            await Assert.That(artSearch.Count).IsEqualTo(1);
            await Assert.That(artSearch[0].ArtAssetPath).IsEqualTo("art/critters/barbarian.art");
            await Assert.That(previewSearch.Count).IsEqualTo(1);
            await Assert.That(previewSearch[0].ArtDetail).IsNotNull();
            await Assert.That(previewSearch[0].ArtPreview).IsNotNull();
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
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));

        try
        {
            ScriptFormat.WriteToFile(
                MakeScriptFile("Indexed target"),
                Path.Combine(contentDir, "scr", "00777Target.scr")
            );

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
            var mapSectors = workspace.Index.FindMapSectors("map01");
            var sectorSummary = workspace.Index.FindSectorSummary("maps/map01/sector.sec");
            var protoDependencies = workspace.Index.FindAssetDependencySummary("proto/001001 - Test.pro");
            var mobDependencies = workspace.Index.FindAssetDependencySummary("mob/test.mob");
            var sectorDependencies = workspace.Index.FindAssetDependencySummary("maps/map01/sector.sec");
            var scriptDependencies = workspace.Index.FindAssetDependencySummary("scr/00777Target.scr");
            var scriptReferences = workspace.Index.FindScriptReferences(scriptId);
            var artReferences = workspace.Index.FindArtReferences(artId);

            await Assert.That(workspace.Index.MapNames.Count).IsEqualTo(1);
            await Assert.That(workspace.Index.MapNames[0]).IsEqualTo("map01");
            await Assert.That(mapAssets.Count).IsEqualTo(1);
            await Assert.That(mapAssets[0].AssetPath).IsEqualTo("maps/map01/sector.sec");
            await Assert.That(assetMap).IsEqualTo("map01");
            await Assert.That(mapSectors.Count).IsEqualTo(1);
            await Assert.That(sectorSummary).IsNotNull();
            await Assert.That(protoDependencies).IsNotNull();
            await Assert.That(mobDependencies).IsNotNull();
            await Assert.That(sectorDependencies).IsNotNull();
            await Assert.That(scriptDependencies).IsNotNull();
            await Assert.That(scriptReferences.Count).IsEqualTo(3);
            await Assert.That(artReferences.Count).IsEqualTo(3);

            await Assert.That(mapSectors[0].Asset.AssetPath).IsEqualTo("maps/map01/sector.sec");
            await Assert.That(mapSectors[0].MapName).IsEqualTo("map01");
            await Assert.That(mapSectors[0].ObjectCount).IsEqualTo(1);
            await Assert.That(mapSectors[0].LightCount).IsEqualTo(1);
            await Assert.That(mapSectors[0].TileScriptCount).IsEqualTo(1);
            await Assert.That(mapSectors[0].SectorScriptId).IsEqualTo(scriptId);
            await Assert.That(mapSectors[0].HasRoofs).IsTrue();
            await Assert.That(mapSectors[0].DistinctTileArtCount).IsEqualTo(2);
            await Assert.That(mapSectors[0].BlockedTileCount).IsEqualTo(0);
            await Assert.That(mapSectors[0].LightSchemeIndex).IsEqualTo(0);
            await Assert.That(mapSectors[0].MusicSchemeIndex).IsEqualTo(-1);
            await Assert.That(mapSectors[0].AmbientSchemeIndex).IsEqualTo(-1);
            await Assert.That(sectorSummary!.Asset.AssetPath).IsEqualTo("maps/map01/sector.sec");
            await Assert.That(protoDependencies!.DefinedProtoNumber).IsEqualTo(protoNumber);
            await Assert.That(protoDependencies.DefinedScriptId.HasValue).IsFalse();
            await Assert.That(protoDependencies.DefinedDialogId.HasValue).IsFalse();
            await Assert.That(protoDependencies.MapName).IsNull();
            await Assert.That(protoDependencies.HasDependencies).IsTrue();
            await Assert.That(protoDependencies.HasIncomingReferences).IsTrue();
            await Assert.That(protoDependencies.HasRelationships).IsTrue();
            await Assert.That(protoDependencies.ProtoReferences.Count).IsEqualTo(0);
            await Assert.That(protoDependencies.ScriptReferences.Count).IsEqualTo(1);
            await Assert.That(protoDependencies.ScriptReferences[0].ScriptId).IsEqualTo(scriptId);
            await Assert.That(protoDependencies.ScriptReferences[0].Count).IsEqualTo(2);
            await Assert.That(protoDependencies.ArtReferences.Count).IsEqualTo(1);
            await Assert.That(protoDependencies.ArtReferences[0].ArtId).IsEqualTo(artId);
            await Assert.That(protoDependencies.ArtReferences[0].Count).IsEqualTo(1);
            await Assert.That(protoDependencies.IncomingProtoReferences.Count).IsEqualTo(2);
            await Assert.That(protoDependencies.IncomingScriptReferences.Count).IsEqualTo(0);
            await Assert.That(mobDependencies!.DefinedProtoNumber.HasValue).IsFalse();
            await Assert.That(mobDependencies.DefinedScriptId.HasValue).IsFalse();
            await Assert.That(mobDependencies.DefinedDialogId.HasValue).IsFalse();
            await Assert.That(mobDependencies.MapName).IsNull();
            await Assert.That(mobDependencies.HasDependencies).IsTrue();
            await Assert.That(mobDependencies.HasIncomingReferences).IsFalse();
            await Assert.That(mobDependencies.ProtoReferences.Count).IsEqualTo(1);
            await Assert.That(mobDependencies.ProtoReferences[0].ProtoNumber).IsEqualTo(protoNumber);
            await Assert.That(mobDependencies.ProtoReferences[0].Count).IsEqualTo(1);
            await Assert.That(mobDependencies.ScriptReferences.Count).IsEqualTo(1);
            await Assert.That(mobDependencies.ScriptReferences[0].ScriptId).IsEqualTo(scriptId);
            await Assert.That(mobDependencies.ScriptReferences[0].Count).IsEqualTo(1);
            await Assert.That(mobDependencies.ArtReferences.Count).IsEqualTo(1);
            await Assert.That(mobDependencies.ArtReferences[0].ArtId).IsEqualTo(artId);
            await Assert.That(mobDependencies.ArtReferences[0].Count).IsEqualTo(1);
            await Assert.That(mobDependencies.IncomingProtoReferences.Count).IsEqualTo(0);
            await Assert.That(mobDependencies.IncomingScriptReferences.Count).IsEqualTo(0);
            await Assert.That(sectorDependencies!.DefinedProtoNumber.HasValue).IsFalse();
            await Assert.That(sectorDependencies.DefinedScriptId.HasValue).IsFalse();
            await Assert.That(sectorDependencies.DefinedDialogId.HasValue).IsFalse();
            await Assert.That(sectorDependencies.MapName).IsEqualTo("map01");
            await Assert.That(sectorDependencies.HasDependencies).IsTrue();
            await Assert.That(sectorDependencies.HasIncomingReferences).IsFalse();
            await Assert.That(sectorDependencies.ProtoReferences.Count).IsEqualTo(1);
            await Assert.That(sectorDependencies.ProtoReferences[0].ProtoNumber).IsEqualTo(protoNumber);
            await Assert.That(sectorDependencies.ProtoReferences[0].Count).IsEqualTo(1);
            await Assert.That(sectorDependencies.ScriptReferences.Count).IsEqualTo(1);
            await Assert.That(sectorDependencies.ScriptReferences[0].ScriptId).IsEqualTo(scriptId);
            await Assert.That(sectorDependencies.ScriptReferences[0].Count).IsEqualTo(3);
            await Assert.That(sectorDependencies.ArtReferences.Count).IsEqualTo(1);
            await Assert.That(sectorDependencies.ArtReferences[0].ArtId).IsEqualTo(artId);
            await Assert.That(sectorDependencies.ArtReferences[0].Count).IsEqualTo(5);
            await Assert.That(sectorDependencies.IncomingProtoReferences.Count).IsEqualTo(0);
            await Assert.That(sectorDependencies.IncomingScriptReferences.Count).IsEqualTo(0);
            await Assert.That(scriptDependencies!.DefinedScriptId).IsEqualTo(scriptId);
            await Assert.That(scriptDependencies.HasDependencies).IsFalse();
            await Assert.That(scriptDependencies.HasIncomingReferences).IsTrue();
            await Assert.That(scriptDependencies.HasRelationships).IsTrue();
            await Assert.That(scriptDependencies.IncomingProtoReferences.Count).IsEqualTo(0);
            await Assert.That(scriptDependencies.IncomingScriptReferences.Count).IsEqualTo(3);

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
            var scriptIncomingReferences = scriptDependencies.IncomingScriptReferences;

            await Assert.That(mobScriptReference.Count).IsEqualTo(1);
            await Assert.That(protoScriptReference.Count).IsEqualTo(2);
            await Assert.That(sectorScriptReference.Count).IsEqualTo(3);
            await Assert.That(mobArtReference.Count).IsEqualTo(1);
            await Assert.That(protoArtReference.Count).IsEqualTo(1);
            await Assert.That(sectorArtReference.Count).IsEqualTo(5);
            await Assert
                .That(scriptIncomingReferences.Select(reference => reference.Asset.AssetPath))
                .IsEquivalentTo(["mob/test.mob", "proto/001001 - Test.pro", "maps/map01/sector.sec"]);
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_IndexesSectorEnvironmentSchemes()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map02"));

        try
        {
            SectorFormat.WriteToFile(MakeSector(3, 11, 22), Path.Combine(contentDir, "maps", "map01", "sector_a.sec"));
            SectorFormat.WriteToFile(MakeSector(7, -1, 22), Path.Combine(contentDir, "maps", "map02", "sector_b.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);

            var light3Sectors = workspace.Index.FindLightSchemeSectors(3);
            var light7Sectors = workspace.Index.FindLightSchemeSectors(7);
            var music11Sectors = workspace.Index.FindMusicSchemeSectors(11);
            var ambient22Sectors = workspace.Index.FindAmbientSchemeSectors(22);
            var ambientMissingSectors = workspace.Index.FindAmbientSchemeSectors(99);

            await Assert.That(light3Sectors.Count).IsEqualTo(1);
            await Assert.That(light3Sectors[0].Asset.AssetPath).IsEqualTo("maps/map01/sector_a.sec");
            await Assert.That(light3Sectors[0].MapName).IsEqualTo("map01");

            await Assert.That(light7Sectors.Count).IsEqualTo(1);
            await Assert.That(light7Sectors[0].Asset.AssetPath).IsEqualTo("maps/map02/sector_b.sec");
            await Assert.That(light7Sectors[0].MapName).IsEqualTo("map02");

            await Assert.That(music11Sectors.Count).IsEqualTo(1);
            await Assert.That(music11Sectors[0].Asset.AssetPath).IsEqualTo("maps/map01/sector_a.sec");

            await Assert.That(ambient22Sectors.Count).IsEqualTo(2);
            await Assert.That(ambient22Sectors[0].Asset.AssetPath).IsEqualTo("maps/map01/sector_a.sec");
            await Assert.That(ambient22Sectors[1].Asset.AssetPath).IsEqualTo("maps/map02/sector_b.sec");

            await Assert.That(ambientMissingSectors.Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_BuildsMapProjectionFromNumericSectorAssetNames()
    {
        const ulong sectorCoordinateMask = (1UL << 26) - 1;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        const ulong northWestSectorKey = 101401495253UL;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{northWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(MakeSector(), Path.Combine(contentDir, "maps", "map01", "sector_notes.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var projection = workspace.Index.FindMapProjection("map01");

            await Assert.That(projection).IsNotNull();

            var mapProjection = projection!;
            var southWest = mapProjection.Sectors.Single(sector =>
                sector.Asset.AssetPath == $"maps/map01/{southWestSectorKey}.sec"
            );
            var southEast = mapProjection.Sectors.Single(sector =>
                sector.Asset.AssetPath == $"maps/map01/{southEastSectorKey}.sec"
            );
            var northWest = mapProjection.Sectors.Single(sector =>
                sector.Asset.AssetPath == $"maps/map01/{northWestSectorKey}.sec"
            );

            await Assert.That(mapProjection.Sectors.Count).IsEqualTo(3);
            await Assert.That(mapProjection.UnpositionedSectorCount).IsEqualTo(1);
            await Assert.That(mapProjection.Width).IsEqualTo(2);
            await Assert.That(mapProjection.Height).IsEqualTo(2);
            await Assert.That(mapProjection.MinSectorX).IsEqualTo((int)(southWestSectorKey & sectorCoordinateMask));
            await Assert.That(mapProjection.MinSectorY).IsEqualTo((int)(southWestSectorKey >> 26));
            await Assert.That(mapProjection.MaxSectorX).IsEqualTo((int)(southEastSectorKey & sectorCoordinateMask));
            await Assert.That(mapProjection.MaxSectorY).IsEqualTo((int)(northWestSectorKey >> 26));

            await Assert.That(southWest.LocalX).IsEqualTo(0);
            await Assert.That(southWest.LocalY).IsEqualTo(0);
            await Assert.That(southEast.LocalX).IsEqualTo(1);
            await Assert.That(southEast.LocalY).IsEqualTo(0);
            await Assert.That(northWest.LocalX).IsEqualTo(0);
            await Assert.That(northWest.LocalY).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_BuildsMapProjectionPreviewFlags()
    {
        const ulong featuredSectorKey = 101334386389UL;
        const ulong plainSectorKey = 101334386390UL;
        const int scriptId = 77;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            var featuredSector = new Sector
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
                        ArtId = 1,
                        R = 0,
                        B = 0,
                        G = 0,
                        TintColor = 0,
                        Palette = 0,
                        Padding2C = 0,
                    },
                ],
                Tiles = new uint[4096],
                HasRoofs = true,
                Roofs = [1u, .. new uint[255]],
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
                BlockMask = [1u, .. new uint[127]],
                Objects = [],
            };

            SectorFormat.WriteToFile(
                featuredSector,
                Path.Combine(contentDir, "maps", "map01", $"{featuredSectorKey}.sec")
            );
            SectorFormat.WriteToFile(MakeSector(), Path.Combine(contentDir, "maps", "map01", $"{plainSectorKey}.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var projection = workspace.Index.FindMapProjection("map01");

            await Assert.That(projection).IsNotNull();

            var featured = projection!.Sectors.Single(sector =>
                sector.Asset.AssetPath == $"maps/map01/{featuredSectorKey}.sec"
            );
            var plain = projection.Sectors.Single(sector =>
                sector.Asset.AssetPath == $"maps/map01/{plainSectorKey}.sec"
            );

            await Assert.That(featured.PreviewFlags).HasFlag(EditorMapSectorPreviewFlags.Occupied);
            await Assert.That(featured.PreviewFlags).HasFlag(EditorMapSectorPreviewFlags.HasRoofs);
            await Assert.That(featured.PreviewFlags).HasFlag(EditorMapSectorPreviewFlags.HasLights);
            await Assert.That(featured.PreviewFlags).HasFlag(EditorMapSectorPreviewFlags.HasBlockedTiles);
            await Assert.That(featured.PreviewFlags).HasFlag(EditorMapSectorPreviewFlags.HasScripts);
            await Assert.That(plain.PreviewFlags).IsEqualTo(EditorMapSectorPreviewFlags.Occupied);
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_BuildsMapProjectionDensityBands()
    {
        const ulong emptySectorKey = 101334386389UL;
        const ulong lowSectorKey = 101334386390UL;
        const ulong mediumSectorKey = 101334386391UL;
        const ulong highSectorKey = 101401495253UL;
        const ulong peakSectorKey = 101401495254UL;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            var emptySector = MakeSector();
            var lowSector = MakeSector(MakePc(10));
            var mediumSector = MakeSector(MakePc(20), MakePc(21));
            var highSector = MakeSector(MakePc(30), MakePc(31), MakePc(32));
            var peakSector = MakeSector(MakePc(40), MakePc(41), MakePc(42), MakePc(43));

            lowSector.BlockMask[0] = 0b1;
            mediumSector.BlockMask[0] = 0b11;
            highSector.BlockMask[0] = 0b111;
            peakSector.BlockMask[0] = 0b1111;

            SectorFormat.WriteToFile(emptySector, Path.Combine(contentDir, "maps", "map01", $"{emptySectorKey}.sec"));
            SectorFormat.WriteToFile(lowSector, Path.Combine(contentDir, "maps", "map01", $"{lowSectorKey}.sec"));
            SectorFormat.WriteToFile(mediumSector, Path.Combine(contentDir, "maps", "map01", $"{mediumSectorKey}.sec"));
            SectorFormat.WriteToFile(highSector, Path.Combine(contentDir, "maps", "map01", $"{highSectorKey}.sec"));
            SectorFormat.WriteToFile(peakSector, Path.Combine(contentDir, "maps", "map01", $"{peakSectorKey}.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var projection = workspace.Index.FindMapProjection("map01");

            await Assert.That(projection).IsNotNull();

            var empty = projection!.Sectors.Single(sector =>
                sector.Asset.AssetPath == $"maps/map01/{emptySectorKey}.sec"
            );
            var low = projection.Sectors.Single(sector => sector.Asset.AssetPath == $"maps/map01/{lowSectorKey}.sec");
            var medium = projection.Sectors.Single(sector =>
                sector.Asset.AssetPath == $"maps/map01/{mediumSectorKey}.sec"
            );
            var high = projection.Sectors.Single(sector => sector.Asset.AssetPath == $"maps/map01/{highSectorKey}.sec");
            var peak = projection.Sectors.Single(sector => sector.Asset.AssetPath == $"maps/map01/{peakSectorKey}.sec");

            await Assert.That(empty.ObjectDensityBand).IsEqualTo(EditorMapSectorDensityBand.None);
            await Assert.That(low.ObjectDensityBand).IsEqualTo(EditorMapSectorDensityBand.Low);
            await Assert.That(medium.ObjectDensityBand).IsEqualTo(EditorMapSectorDensityBand.Medium);
            await Assert.That(high.ObjectDensityBand).IsEqualTo(EditorMapSectorDensityBand.High);
            await Assert.That(peak.ObjectDensityBand).IsEqualTo(EditorMapSectorDensityBand.Peak);

            await Assert.That(empty.BlockedTileDensityBand).IsEqualTo(EditorMapSectorDensityBand.None);
            await Assert.That(low.BlockedTileDensityBand).IsEqualTo(EditorMapSectorDensityBand.Low);
            await Assert.That(medium.BlockedTileDensityBand).IsEqualTo(EditorMapSectorDensityBand.Medium);
            await Assert.That(high.BlockedTileDensityBand).IsEqualTo(EditorMapSectorDensityBand.High);
            await Assert.That(peak.BlockedTileDensityBand).IsEqualTo(EditorMapSectorDensityBand.Peak);
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_CreateMapScenePreview_UsesLoadedSectorPayloads()
    {
        const ulong sectorKey = 101334386389UL;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 1, Guid.NewGuid());
            var mob = new CharacterBuilder(ObjectType.Pc, objectId, MakeProtoId(1)).WithLocation(10, 11).Build();
            var tiles = new uint[4096];
            tiles[(6 * 64) + 5] = 0x11223344u;
            var roofs = new uint[256];
            roofs[(3 * 16) + 2] = 0x55667788u;
            var blockMask = new uint[128];
            blockMask[4] = 1u << 5;

            var sector = new Sector
            {
                Lights =
                [
                    new SectorLight
                    {
                        ObjHandle = -1,
                        TileLoc = ((long)4 << 32) | 3u,
                        OffsetX = 0,
                        OffsetY = 0,
                        Flags = SectorLightFlags.Off,
                        ArtId = 0x01020304u,
                        R = 9,
                        B = 10,
                        G = 11,
                        TintColor = 12u,
                        Palette = 13,
                        Padding2C = 0,
                    },
                ],
                Tiles = tiles,
                HasRoofs = true,
                Roofs = roofs,
                SectorScript = null,
                TileScripts =
                [
                    new TileScript
                    {
                        NodeFlags = 1u,
                        TileId = 65u,
                        ScriptFlags = 2u,
                        ScriptCounters = 3u,
                        ScriptNum = 77,
                    },
                ],
                TownmapInfo = 0,
                AptitudeAdjustment = 0,
                LightSchemeIdx = 0,
                SoundList = SectorSoundList.Default,
                BlockMask = blockMask,
                Objects = [mob],
            };

            SectorFormat.WriteToFile(sector, Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var preview = workspace.CreateMapScenePreview("map01");

            await Assert.That(preview.MapName).IsEqualTo("map01");
            await Assert.That(preview.Width).IsEqualTo(1);
            await Assert.That(preview.Height).IsEqualTo(1);
            await Assert.That(preview.Sectors.Count).IsEqualTo(1);
            await Assert.That(preview.Sectors[0].AssetPath).IsEqualTo($"maps/map01/{sectorKey}.sec");
            await Assert.That(preview.Sectors[0].GetTileArtId(5, 6)).IsEqualTo(0x11223344u);
            await Assert.That(preview.Sectors[0].GetRoofArtId(2, 3)).IsEqualTo(0x55667788u);
            await Assert.That(preview.Sectors[0].IsTileBlocked(5, 2)).IsTrue();
            await Assert.That(preview.Sectors[0].TileScripts.Count).IsEqualTo(1);
            await Assert.That(preview.Sectors[0].Lights.Count).IsEqualTo(1);
            await Assert.That(preview.Sectors[0].Objects.Count).IsEqualTo(1);
            await Assert.That(preview.Sectors[0].Objects[0].Location).IsEqualTo(new Location(10, 11));
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
    public async Task LoadAsync_LoadsOptionalSaveIntoWorkspace_WhenGsiUsesDecoratedSlotName()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var saveDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(contentDir);
        Directory.CreateDirectory(saveDir);

        try
        {
            var mes = new MesFile { Entries = [new MessageEntry(20, "Workspace with decorated save")] };
            MessageFormat.WriteToFile(in mes, Path.Combine(contentDir, "game.mes"));

            var save = MakeMinimalSave();
            SaveGameWriter.Save(save, saveDir, "slot0001");
            File.Move(Path.Combine(saveDir, "slot0001.gsi"), Path.Combine(saveDir, "slot0001WorkspacePc.gsi"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(
                contentDir,
                new EditorWorkspaceLoadOptions { SaveFolder = saveDir, SaveSlotName = "slot0001" }
            );

            await Assert.That(workspace.HasSaveLoaded).IsTrue();
            await Assert.That(workspace.Save).IsNotNull();
            await Assert.That(workspace.Save!.Info.LeaderName).IsEqualTo("WorkspacePc");
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
    public async Task LoadFromGameInstallAsync_LoadsFacadeWalkAssetsFromModuleDat()
    {
        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(gameDir);
        Directory.CreateDirectory(Path.Combine(gameDir, "modules"));

        try
        {
            var facadeWalk = new FacadeWalk
            {
                Header = new FacWalkHeader(22, 1, 0, 8, 6),
                Entries = [new FacWalkEntry(1, 2, true), new FacWalkEntry(3, 4, false)],
            };

            var archivePath = Path.Combine(gameDir, "modules", "Arcanum.dat");
            await WriteDatAsync(
                archivePath,
                new Dictionary<string, byte[]> { ["walk\\facwalk.test"] = FacWalkFormat.WriteToArray(in facadeWalk) }
            );

            var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(gameDir);
            var asset = workspace.Assets.Find("walk/facwalk.test");
            var detail = workspace.Index.FindFacadeWalkDetail("walk/facwalk.test");

            await Assert.That(workspace.Assets.FindByFormat(FileFormat.FacadeWalk).Count).IsEqualTo(1);
            await Assert.That(workspace.FindFacadeWalk("walk/facwalk.test")?.Entries.Length).IsEqualTo(2);
            await Assert.That(asset).IsNotNull();
            await Assert.That(asset!.SourceKind).IsEqualTo(EditorAssetSourceKind.DatArchive);
            await Assert.That(asset.SourcePath).IsEqualTo(archivePath);
            await Assert.That(asset.SourceEntryPath).IsEqualTo("walk/facwalk.test");
            await Assert.That(detail).IsNotNull();
            await Assert.That(detail!.Terrain).IsEqualTo(22u);
            await Assert.That(detail.WalkableEntryCount).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadFromGameInstallAsync_NormalizesWrapperRootWithNestedGameDirectory()
    {
        var wrapperDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var gameDir = Path.Combine(wrapperDir, "Arcanum");
        Directory.CreateDirectory(gameDir);
        Directory.CreateDirectory(Path.Combine(gameDir, "modules"));

        try
        {
            var mes = new MesFile { Entries = [new MessageEntry(31, "Nested archive workspace")] };
            var archivePath = Path.Combine(gameDir, "modules", "Arcanum.dat");
            await WriteDatAsync(
                archivePath,
                new Dictionary<string, byte[]> { ["mes\\game.mes"] = MessageFormat.WriteToArray(in mes) }
            );

            var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(wrapperDir);
            var asset = workspace.Assets.Find("mes/game.mes");

            await Assert.That(workspace.GameDirectory).IsEqualTo(gameDir);
            await Assert.That(workspace.ContentDirectory).IsEqualTo(Path.Combine(gameDir, "data"));
            await Assert.That(workspace.GameData.Messages.Count).IsEqualTo(1);
            await Assert.That(workspace.GameData.Messages[0].Text).IsEqualTo("Nested archive workspace");
            await Assert.That(asset).IsNotNull();
            await Assert.That(asset!.SourceKind).IsEqualTo(EditorAssetSourceKind.DatArchive);
            await Assert.That(asset.SourcePath).IsEqualTo(archivePath);
        }
        finally
        {
            Directory.Delete(wrapperDir, recursive: true);
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
    public async Task LoadFromGameInstallAsync_LoadsArtAssetsFromArchiveAndBuildsWorkspacePreview()
    {
        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(gameDir);
        Directory.CreateDirectory(Path.Combine(gameDir, "modules"));

        try
        {
            var archivePath = Path.Combine(gameDir, "modules", "Arcanum.dat");
            var artFile = MakeArtFile(frameRate: 15);
            await WriteDatAsync(
                archivePath,
                new Dictionary<string, byte[]> { ["art\\critters\\barbarian.art"] = ArtFormat.WriteToArray(in artFile) }
            );

            var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(gameDir);
            var asset = workspace.Assets.Find("art/critters/barbarian.art");
            var preview = workspace.CreateArtPreview("art/critters/barbarian.art");

            await Assert.That(workspace.GameData.Arts.Count).IsEqualTo(1);
            await Assert.That(asset).IsNotNull();
            await Assert.That(asset!.SourceKind).IsEqualTo(EditorAssetSourceKind.DatArchive);
            await Assert.That(asset.SourcePath).IsEqualTo(archivePath);
            await Assert.That(asset.SourceEntryPath).IsEqualTo("art/critters/barbarian.art");
            await Assert.That(preview.FrameRate).IsEqualTo(15u);
            await Assert.That(preview.Frames.Count).IsEqualTo(1);
            await Assert.That(preview.Frames[0].PixelData.SequenceEqual(new byte[] { 3, 2, 1, 255 })).IsTrue();
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
    public async Task LoadFromGameInstallAsync_WithModuleName_LoadsLooseAndPackedModuleContextAndBootstrapsDefaultScene()
    {
        const int protoNumber = 1001;
        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var modulesDir = Path.Combine(gameDir, "modules");
        var moduleDir = Path.Combine(modulesDir, "Arcanum");
        Directory.CreateDirectory(Path.Combine(moduleDir, "maps", "map01"));
        Directory.CreateDirectory(Path.Combine(moduleDir, "Save"));

        try
        {
            var artFile = MakeArtFile(frameRate: 12);
            await WriteDatAsync(
                Path.Combine(modulesDir, "Arcanum.dat"),
                new Dictionary<string, byte[]>
                {
                    ["proto\\001001 - Test.pro"] = ProtoFormat.WriteToArray(
                        WithProperties(MakeProto(protoNumber), MakeArtProperty(ObjectField.ObjFCurrentAid, 200u))
                    ),
                    ["mes\\description.mes"] = MessageFormat.WriteToArray(
                        new MesFile { Entries = [new MessageEntry(protoNumber, "Base palette proto")] }
                    ),
                    ["art\\critters\\barbarian.art"] = ArtFormat.WriteToArray(in artFile),
                }
            );
            await WriteDatAsync(
                Path.Combine(modulesDir, "Arcanum.PATCH0"),
                new Dictionary<string, byte[]>
                {
                    ["mes\\description.mes"] = MessageFormat.WriteToArray(
                        new MesFile { Entries = [new MessageEntry(protoNumber, "Patched palette proto")] }
                    ),
                }
            );
            SectorFormat.WriteToFile(
                new SectorBuilder(MakeSector()).SetTile(0, 0, 100u).Build(),
                Path.Combine(moduleDir, "maps", "map01", "0.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(
                gameDir,
                new EditorWorkspaceLoadOptions { ModuleName = "Arcanum" }
            );
            var artResolver = workspace.CreateArtResolver();
            artResolver.Bind(new ArtId(100u), "art/critters/barbarian.art");
            var paletteEntry = workspace.FindObjectPaletteEntry(protoNumber, artResolver);
            var session = workspace.CreateSession();
            var worldScene = session.CreateDefaultMapWorldEditScene(
                request: new EditorMapWorldEditSceneRequest
                {
                    RenderRequest = new EditorMapFloorRenderRequest
                    {
                        ViewMode = EditorMapSceneViewMode.Isometric,
                        TileWidthPixels = 64d,
                        TileHeightPixels = 32d,
                    },
                    ArtResolver = artResolver,
                }
            );

            await Assert.That(workspace.GameDirectory).IsEqualTo(gameDir);
            await Assert.That(workspace.ContentDirectory).IsEqualTo(moduleDir);
            await Assert.That(workspace.Module).IsNotNull();
            await Assert.That(workspace.Module!.ModuleName).IsEqualTo("Arcanum");
            await Assert.That(workspace.Module.ModuleDirectory).IsEqualTo(moduleDir);
            await Assert.That(workspace.Module.SaveDirectory).IsEqualTo(Path.Combine(moduleDir, "Save"));
            await Assert.That(workspace.Module.ArchivePaths.Count).IsEqualTo(2);
            await Assert.That(workspace.ResolveDefaultMap()!.MapName).IsEqualTo("map01");
            await Assert.That(paletteEntry).IsNotNull();
            await Assert.That(paletteEntry!.DisplayName).IsEqualTo("Patched palette proto");
            await Assert.That(workspace.SearchObjectPalette("Patched").Count).IsEqualTo(1);
            await Assert.That(worldScene.MapName).IsEqualTo("map01");
            await Assert.That(worldScene.SceneRender.Tiles.Count).IsEqualTo(1);
            await Assert.That(worldScene.SpriteCoverage.IsComplete).IsTrue();
            await Assert.That(worldScene.PaintableScene.Items.Any(item => item.Sprite is not null)).IsTrue();
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadFromModuleDirectoryAsync_LoadsSiblingArchivesAndCapturesModuleProjectReference()
    {
        const int protoNumber = 1001;
        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var modulesDir = Path.Combine(gameDir, "modules");
        var moduleDir = Path.Combine(modulesDir, "Arcanum");
        Directory.CreateDirectory(Path.Combine(moduleDir, "maps", "map01"));

        try
        {
            await WriteDatAsync(
                Path.Combine(modulesDir, "Arcanum.dat"),
                new Dictionary<string, byte[]>
                {
                    ["proto\\001001 - Test.pro"] = ProtoFormat.WriteToArray(MakeProto(protoNumber)),
                    ["mes\\description.mes"] = MessageFormat.WriteToArray(
                        new MesFile { Entries = [new MessageEntry(protoNumber, "Directory palette proto")] }
                    ),
                }
            );
            SectorFormat.WriteToFile(
                new SectorBuilder(MakeSector()).SetTile(0, 0, 100u).Build(),
                Path.Combine(moduleDir, "maps", "map01", "0.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadFromModuleDirectoryAsync(moduleDir);
            var project = workspace.CreateProject();

            await Assert.That(workspace.GameDirectory).IsEqualTo(gameDir);
            await Assert.That(workspace.ContentDirectory).IsEqualTo(moduleDir);
            await Assert.That(workspace.Module).IsNotNull();
            await Assert.That(workspace.Module!.ModuleName).IsEqualTo("Arcanum");
            await Assert.That(workspace.FindObjectPaletteEntry(protoNumber)).IsNotNull();
            await Assert.That(project.Workspace.Kind).IsEqualTo(EditorProjectWorkspaceKind.GameInstall);
            await Assert.That(project.Workspace.RootPath).IsEqualTo(gameDir);
            await Assert.That(project.Workspace.ModuleName).IsEqualTo("Arcanum");
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
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
