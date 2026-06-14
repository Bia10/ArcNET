using System.Buffers.Binary;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.GameData.Workspace.Tests;

internal static class WorkspaceCatalogTestData
{
    public static GameObjectGuid MakeProtoReference(int protoNumber)
    {
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, protoNumber);
        return new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, new Guid(bytes));
    }

    public static ProtoData MakePrototype(ObjectType type, int protoNumber, params ObjectProperty[] properties)
    {
        byte[] bitmap = new byte[ObjectFieldBitmapSize.For(type)];
        for (var propertyIndex = 0; propertyIndex < properties.Length; propertyIndex++)
            bitmap.SetField(properties[propertyIndex].Field, true);

        return new ProtoData
        {
            Header = new GameObjectHeader
            {
                Version = 0x77,
                ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeBlocked, 0, 0, Guid.Empty),
                ObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid()),
                GameObjectType = type,
                PropCollectionItems = 0,
                Bitmap = bitmap,
            },
            Properties = [.. properties.OrderBy(static property => property.Field)],
        };
    }

    public static MobData MakeMob(
        ObjectType type,
        int protoNumber,
        Guid? objectGuid = null,
        int? tileX = null,
        int? tileY = null,
        params ObjectProperty[] properties
    )
    {
        List<ObjectProperty> resolvedProperties = [.. properties];
        if (tileX.HasValue && tileY.HasValue)
            resolvedProperties.Add(ObjectPropertyFactory.ForLocation(ObjectField.Location, tileX.Value, tileY.Value));

        byte[] bitmap = new byte[ObjectFieldBitmapSize.For(type)];
        for (var propertyIndex = 0; propertyIndex < resolvedProperties.Count; propertyIndex++)
            bitmap.SetField(resolvedProperties[propertyIndex].Field, true);

        return new MobData
        {
            Header = new GameObjectHeader
            {
                Version = 0x77,
                ProtoId = MakeProtoReference(protoNumber),
                ObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, objectGuid ?? Guid.NewGuid()),
                GameObjectType = type,
                PropCollectionItems = checked((short)resolvedProperties.Count),
                Bitmap = bitmap,
            },
            Properties = [.. resolvedProperties.OrderBy(static property => property.Field)],
        };
    }

    public static Sector MakeSector(params MobData[] objects) =>
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

    public static ArtFile MakeArtFile(
        uint frameRate = 8,
        byte paletteIndex = 1,
        int width = 1,
        int height = 1,
        int centerX = 0,
        int centerY = 0
    ) =>
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
                [
                    new ArtFrame
                    {
                        Header = new ArtFrameHeader(
                            checked((uint)width),
                            checked((uint)height),
                            checked((uint)(width * height)),
                            centerX,
                            centerY,
                            0,
                            0
                        ),
                        Pixels = Enumerable.Repeat(paletteIndex, checked(width * height)).ToArray(),
                    },
                ],
            ],
        };

    private static ArtPaletteEntry[] CreateArtPalette()
    {
        var palette = new ArtPaletteEntry[256];
        palette[1] = new ArtPaletteEntry(1, 2, 3);
        return palette;
    }
}
