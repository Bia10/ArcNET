using System.Buffers.Binary;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor.Tests;

public sealed class EditorMapScenePreviewBuilderTests
{
    private static GameObjectGuid MakeProtoId(int protoNumber) =>
        new(GameObjectGuid.OidTypeA, 0, 0, CreateProtoGuid(protoNumber));

    private static Guid CreateProtoGuid(int protoNumber)
    {
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, protoNumber);
        return new Guid(bytes);
    }

    private static ArtFile CreateArtFile(params ArtFrameHeader[] frameHeaders)
    {
        var palette = new ArtPaletteEntry[256];
        palette[1] = new ArtPaletteEntry(0, 0, 255);

        return new ArtFile
        {
            Flags = ArtFlags.Static,
            FrameRate = 8,
            ActionFrame = 0,
            FrameCount = checked((uint)frameHeaders.Length),
            DataSizes = new uint[8],
            PaletteData1 = new uint[8],
            PaletteData2 = new uint[8],
            PaletteIds = [1, 0, 0, 0],
            Palettes = [palette, null, null, null],
            Frames =
            [
                [
                    .. frameHeaders.Select(header => new ArtFrame
                    {
                        Header = header,
                        Pixels = new byte[checked((int)(header.Width * header.Height))],
                    }),
                ],
            ],
        };
    }

    [Test]
    public async Task BuildSector_ProjectsTileRoofBlockLightScriptAndObjectMarkers()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 1, Guid.NewGuid());
        var mob = new CharacterBuilder(ObjectType.Pc, objectId, MakeProtoId(1))
            .WithLocation(10, 11)
            .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFOffsetX, 3))
            .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFOffsetY, 4))
            .WithProperty(ObjectPropertyFactory.ForFloat(ObjectField.ObjFOffsetZ, 5.5f))
            .WithProperty(ObjectPropertyFactory.ForFloat(ObjectField.ObjFHeight, 6.5f))
            .Build();

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
                    OffsetX = 7,
                    OffsetY = 8,
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

        var preview = EditorMapScenePreviewBuilder.BuildSector(
            new EditorMapSectorProjection
            {
                Sector = new EditorSectorSummary
                {
                    Asset = new EditorAssetEntry
                    {
                        AssetPath = "maps/map01/sector.sec",
                        Format = FileFormat.Sector,
                        ItemCount = 1,
                        SourceKind = EditorAssetSourceKind.LooseFile,
                        SourcePath = "test/sector.sec",
                    },
                    MapName = "map01",
                    ObjectCount = 1,
                    LightCount = 1,
                    TileScriptCount = 1,
                    SectorScriptId = null,
                    HasRoofs = true,
                    DistinctTileArtCount = 2,
                    BlockedTileCount = 1,
                    LightSchemeIndex = 0,
                    MusicSchemeIndex = -1,
                    AmbientSchemeIndex = -1,
                },
                SectorX = 20,
                SectorY = 30,
                LocalX = 1,
                LocalY = 2,
                ObjectDensityBand = EditorMapSectorDensityBand.Low,
                BlockedTileDensityBand = EditorMapSectorDensityBand.Medium,
            },
            sector
        );

        await Assert.That(preview.AssetPath).IsEqualTo("maps/map01/sector.sec");
        await Assert.That(preview.TileWidth).IsEqualTo(64);
        await Assert.That(preview.TileHeight).IsEqualTo(64);
        await Assert.That(preview.RoofWidth).IsEqualTo(16);
        await Assert.That(preview.RoofHeight).IsEqualTo(16);
        await Assert.That(preview.GetTileArtId(5, 6)).IsEqualTo(0x11223344u);
        await Assert.That(preview.GetRoofArtId(2, 3)).IsEqualTo(0x55667788u);
        await Assert.That(preview.IsTileBlocked(5, 2)).IsTrue();
        await Assert.That(preview.IsTileBlocked(4, 2)).IsFalse();
        await Assert.That(preview.Lights.Count).IsEqualTo(1);
        await Assert.That(preview.Lights[0].TileX).IsEqualTo(3);
        await Assert.That(preview.Lights[0].TileY).IsEqualTo(4);
        await Assert.That(preview.Lights[0].ArtId).IsEqualTo(new ArtId(0x01020304u));
        await Assert.That(preview.TileScripts.Count).IsEqualTo(1);
        await Assert.That(preview.TileScripts[0].TileX).IsEqualTo(1);
        await Assert.That(preview.TileScripts[0].TileY).IsEqualTo(1);
        await Assert.That(preview.TileScripts[0].ScriptId).IsEqualTo(77);
        await Assert.That(preview.Objects.Count).IsEqualTo(1);
        await Assert.That(preview.Objects[0].ObjectType).IsEqualTo(ObjectType.Pc);
        await Assert.That(preview.Objects[0].Location).IsEqualTo(new Location(10, 11));
        await Assert.That(preview.Objects[0].OffsetX).IsEqualTo(3);
        await Assert.That(preview.Objects[0].OffsetY).IsEqualTo(4);
        await Assert.That(preview.Objects[0].OffsetZ).IsEqualTo(5.5f);
        await Assert.That(preview.Objects[0].CollisionHeight).IsEqualTo(6.5f);
        await Assert.That(preview.Objects[0].IsTileGridSnapped).IsFalse();
        await Assert.That(preview.Objects[0].SpriteBounds).IsNull();
    }

    [Test]
    public async Task BuildSector_WithArtResolver_ProjectsConservativeSpriteBounds()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 2, Guid.NewGuid());
        var artId = new ArtId(0x01020304u);
        var mob = new CharacterBuilder(ObjectType.Pc, objectId, MakeProtoId(1))
            .WithLocation(10, 11)
            .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFCurrentAid, unchecked((int)artId.Value)))
            .Build();
        var sector = new Sector
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
            Objects = [mob],
        };
        var art = CreateArtFile(
            new ArtFrameHeader(6u, 7u, 42u, 2, 3, 0, 0),
            new ArtFrameHeader(9u, 11u, 99u, 4, 8, 0, 0)
        );

        var preview = EditorMapScenePreviewBuilder.BuildSector(
            new EditorMapSectorProjection
            {
                Sector = new EditorSectorSummary
                {
                    Asset = new EditorAssetEntry
                    {
                        AssetPath = "maps/map01/sector.sec",
                        Format = FileFormat.Sector,
                        ItemCount = 1,
                        SourceKind = EditorAssetSourceKind.LooseFile,
                        SourcePath = "test/sector.sec",
                    },
                    MapName = "map01",
                    ObjectCount = 1,
                    LightCount = 0,
                    TileScriptCount = 0,
                    SectorScriptId = null,
                    HasRoofs = false,
                    DistinctTileArtCount = 0,
                    BlockedTileCount = 0,
                    LightSchemeIndex = 0,
                    MusicSchemeIndex = -1,
                    AmbientSchemeIndex = -1,
                },
                SectorX = 20,
                SectorY = 30,
                LocalX = 1,
                LocalY = 2,
                ObjectDensityBand = EditorMapSectorDensityBand.Low,
                BlockedTileDensityBand = EditorMapSectorDensityBand.None,
            },
            sector,
            resolvedArtId => resolvedArtId == artId ? art : null
        );

        await Assert.That(preview.Objects.Count).IsEqualTo(1);
        await Assert.That(preview.Objects[0].CurrentArtId).IsEqualTo(artId);
        await Assert.That(preview.Objects[0].IsTileGridSnapped).IsTrue();
        await Assert.That(preview.Objects[0].SpriteBounds).IsNotNull();
        await Assert.That(preview.Objects[0].SpriteBounds!.MaxFrameWidth).IsEqualTo(9);
        await Assert.That(preview.Objects[0].SpriteBounds!.MaxFrameHeight).IsEqualTo(11);
        await Assert.That(preview.Objects[0].SpriteBounds!.MaxFrameCenterX).IsEqualTo(4);
        await Assert.That(preview.Objects[0].SpriteBounds!.MaxFrameCenterY).IsEqualTo(8);
    }

    [Test]
    public async Task BuildObjectPreview_ProjectsNpcProperties_WhenNpcCarriesFollowerArrayField()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 3, Guid.NewGuid());
        var artId = new ArtId(0x01020304u);
        var mob = CreateEmptyMob(ObjectType.Npc, objectId, MakeProtoId(1))
            .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFCurrentAid, unchecked((int)artId.Value)))
            .WithProperty(ObjectPropertyFactory.ForLocation(ObjectField.ObjFLocation, 10, 11))
            .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFOffsetX, 3))
            .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFOffsetY, 4))
            .WithProperty(ObjectPropertyFactory.ForFloat(ObjectField.ObjFOffsetZ, 5.5f))
            .WithProperty(ObjectPropertyFactory.ForFloat(ObjectField.ObjFHeight, 6.5f))
            .WithProperty(ObjectPropertyFactory.ForEmptyObjectIdArray(ObjectField.ObjFCritterFollowerIdx));

        var preview = EditorMapScenePreviewBuilder.BuildObjectPreview(mob);

        await Assert.That(preview.ObjectId).IsEqualTo(objectId);
        await Assert.That(preview.ProtoId).IsEqualTo(MakeProtoId(1));
        await Assert.That(preview.ObjectType).IsEqualTo(ObjectType.Npc);
        await Assert.That(preview.CurrentArtId).IsEqualTo(artId);
        await Assert.That(preview.Location).IsEqualTo(new Location(10, 11));
        await Assert.That(preview.OffsetX).IsEqualTo(3);
        await Assert.That(preview.OffsetY).IsEqualTo(4);
        await Assert.That(preview.OffsetZ).IsEqualTo(5.5f);
        await Assert.That(preview.CollisionHeight).IsEqualTo(6.5f);
    }

    private static MobData CreateEmptyMob(ObjectType objectType, GameObjectGuid objectId, GameObjectGuid protoId) =>
        new()
        {
            Header = new GameObjectHeader
            {
                Version = 0x08,
                ProtoId = protoId,
                ObjectId = objectId,
                GameObjectType = objectType,
                PropCollectionItems = 0,
                Bitmap = new byte[ObjectFieldBitmapSize.For(objectType)],
            },
            Properties = [],
        };
}
