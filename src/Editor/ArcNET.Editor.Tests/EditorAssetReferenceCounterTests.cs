using System.Buffers.Binary;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameData;
using ArcNET.GameObjects;

namespace ArcNET.Editor.Tests;

public sealed class EditorAssetReferenceCounterTests
{
    [Test]
    public async Task CountReferences_CombinesMobProtoSectorAndTerrainReferences()
    {
        const string MobAssetPath = "maps/map01/mobile/G_test.mob";
        const string ProtoAssetPath = "proto/003000 - Test.pro";
        const string SectorAssetPath = "maps/map01/sector.sec";
        const string SecondarySectorAssetPath = "maps/map02/sector.sec";
        var mob = MakeMob(
            protoNumber: 1001,
            MakeScriptProperty(2001, 2001, 0),
            MakeArtProperty(ObjectField.CurrentAid, 3001),
            MakeArtProperty(ObjectField.Shadow, 3001)
        );
        var proto = MakeProto(
            protoNumber: 3000,
            MakeLegacyScriptProperty(2002, -1, 0),
            MakeArtProperty(ObjectField.Aid, 3002)
        );
        var sectorObject = MakeMob(
            protoNumber: 1002,
            MakeScriptProperty(2003),
            MakeArtProperty(ObjectField.DestroyedAid, 3003)
        );
        var sector = new SectorBuilder()
            .SetTile(0, 0, 4001)
            .SetTile(1, 0, 4001)
            .SetTile(2, 0, 4002)
            .SetRoof(0, 0, 5001)
            .AddLight(MakeLight(6001))
            .AddTileScript(MakeTileScript(2004))
            .WithSectorScript(new GameObjectScript(0u, 0, 2005))
            .AddObject(sectorObject)
            .Build();
        var secondarySector = new SectorBuilder()
            .SetTile(0, 0, 4001)
            .WithSectorScript(new GameObjectScript(0u, 0, 2005))
            .AddObject(MakeMob(protoNumber: 1002))
            .Build();
        var gameData = GameDataStoreSnapshotBuilder.CloneWithAssetReplacements(
            new GameDataStore(),
            updatedSectors: new Dictionary<string, Sector>(StringComparer.OrdinalIgnoreCase)
            {
                [SectorAssetPath] = sector,
                [SecondarySectorAssetPath] = secondarySector,
            },
            updatedProtos: new Dictionary<string, ProtoData>(StringComparer.OrdinalIgnoreCase)
            {
                [ProtoAssetPath] = proto,
            },
            updatedMobs: new Dictionary<string, MobData>(StringComparer.OrdinalIgnoreCase) { [MobAssetPath] = mob }
        );
        var assetsByPath = new Dictionary<string, EditorAssetEntry>(StringComparer.OrdinalIgnoreCase)
        {
            [MobAssetPath] = Asset(MobAssetPath, FileFormat.Mob),
            [ProtoAssetPath] = Asset(ProtoAssetPath, FileFormat.Proto),
            [SectorAssetPath] = Asset(SectorAssetPath, FileFormat.Sector),
            [SecondarySectorAssetPath] = Asset(SecondarySectorAssetPath, FileFormat.Sector),
        };

        EditorAssetReferenceCounter.CountReferences(
            gameData,
            assetsByPath,
            out var protoReferencesByNumber,
            out var protoReferencesByAssetPath,
            out var scriptReferencesById,
            out var scriptReferencesByAssetPath,
            out var artReferencesById,
            out var artReferencesByAssetPath
        );

        await AssertReference(protoReferencesByNumber[1001].Single(), MobAssetPath, count: 1);
        await AssertReferenceSequence(
            protoReferencesByNumber[1002],
            [(SectorAssetPath, 1), (SecondarySectorAssetPath, 1)]
        );
        await Assert.That(protoReferencesByAssetPath[MobAssetPath].Single().ProtoNumber).IsEqualTo(1001);
        await Assert.That(protoReferencesByAssetPath[SectorAssetPath].Single().ProtoNumber).IsEqualTo(1002);

        await AssertReference(scriptReferencesById[2001].Single(), MobAssetPath, count: 2);
        await AssertReference(scriptReferencesById[2002].Single(), ProtoAssetPath, count: 1);
        await AssertReference(scriptReferencesById[2003].Single(), SectorAssetPath, count: 1);
        await AssertReference(scriptReferencesById[2004].Single(), SectorAssetPath, count: 1);
        await AssertReferenceSequence(
            scriptReferencesById[2005],
            [(SectorAssetPath, 1), (SecondarySectorAssetPath, 1)]
        );
        await Assert.That(scriptReferencesById.ContainsKey(-1)).IsFalse();
        await Assert.That(scriptReferencesById.ContainsKey(0)).IsFalse();
        await Assert
            .That(scriptReferencesByAssetPath[SectorAssetPath].Select(static reference => reference.ScriptId))
            .IsEquivalentTo([2003, 2004, 2005]);

        await AssertReference(artReferencesById[3001].Single(), MobAssetPath, count: 2);
        await AssertReference(artReferencesById[3002].Single(), ProtoAssetPath, count: 1);
        await AssertReference(artReferencesById[3003].Single(), SectorAssetPath, count: 1);
        await AssertReferenceSequence(artReferencesById[4001], [(SectorAssetPath, 2), (SecondarySectorAssetPath, 1)]);
        await AssertReference(artReferencesById[4002].Single(), SectorAssetPath, count: 1);
        await AssertReference(artReferencesById[5001].Single(), SectorAssetPath, count: 1);
        await AssertReference(artReferencesById[6001].Single(), SectorAssetPath, count: 1);
        await Assert
            .That(artReferencesByAssetPath[SectorAssetPath].Select(static reference => reference.ArtId))
            .IsEquivalentTo([3003u, 4001u, 4002u, 5001u, 6001u]);
    }

    private static async Task AssertReferenceSequence(
        IReadOnlyList<EditorProtoReference> references,
        IReadOnlyList<(string AssetPath, int Count)> expected
    )
    {
        await Assert
            .That(
                references
                    .Select(static reference => (reference.Asset.AssetPath, reference.Count))
                    .SequenceEqual(expected)
            )
            .IsTrue();
    }

    private static async Task AssertReferenceSequence(
        IReadOnlyList<EditorScriptReference> references,
        IReadOnlyList<(string AssetPath, int Count)> expected
    )
    {
        await Assert
            .That(
                references
                    .Select(static reference => (reference.Asset.AssetPath, reference.Count))
                    .SequenceEqual(expected)
            )
            .IsTrue();
    }

    private static async Task AssertReferenceSequence(
        IReadOnlyList<EditorArtReference> references,
        IReadOnlyList<(string AssetPath, int Count)> expected
    )
    {
        await Assert
            .That(
                references
                    .Select(static reference => (reference.Asset.AssetPath, reference.Count))
                    .SequenceEqual(expected)
            )
            .IsTrue();
    }

    private static async Task AssertReference(EditorProtoReference reference, string assetPath, int count)
    {
        await Assert.That(reference.Asset.AssetPath).IsEqualTo(assetPath);
        await Assert.That(reference.Count).IsEqualTo(count);
    }

    private static async Task AssertReference(EditorScriptReference reference, string assetPath, int count)
    {
        await Assert.That(reference.Asset.AssetPath).IsEqualTo(assetPath);
        await Assert.That(reference.Count).IsEqualTo(count);
    }

    private static async Task AssertReference(EditorArtReference reference, string assetPath, int count)
    {
        await Assert.That(reference.Asset.AssetPath).IsEqualTo(assetPath);
        await Assert.That(reference.Count).IsEqualTo(count);
    }

    private static EditorAssetEntry Asset(string assetPath, FileFormat format) =>
        new()
        {
            AssetPath = assetPath,
            Format = format,
            ItemCount = 1,
            SourceKind = EditorAssetSourceKind.LooseFile,
            SourcePath = assetPath,
        };

    private static MobData MakeMob(int protoNumber, params ObjectProperty[] properties)
    {
        var builder = new MobDataBuilder(
            ObjectType.Scenery,
            new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid()),
            MakeProtoId(protoNumber)
        );
        foreach (var property in properties)
            builder.WithProperty(property);

        return builder.Build();
    }

    private static ProtoData MakeProto(int protoNumber, params ObjectProperty[] properties)
    {
        var mob = MakeMob(protoNumber, properties);
        return new ProtoData
        {
            Header = new GameObjectHeader
            {
                Version = mob.Header.Version,
                ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeBlocked, 0, 0, Guid.Empty),
                ObjectId = mob.Header.ObjectId,
                GameObjectType = mob.Header.GameObjectType,
                PropCollectionItems = 0,
                Bitmap = [.. mob.Header.Bitmap],
            },
            Properties = mob.Properties,
        };
    }

    private static ObjectProperty MakeScriptProperty(params int[] scriptIds) =>
        new ObjectProperty { Field = ObjectField.ScriptsIdx, RawBytes = [0] }.WithScriptArray([
            .. scriptIds.Select(static scriptId => new ObjectPropertyScript(0u, 0u, scriptId)),
        ]);

    private static ObjectProperty MakeLegacyScriptProperty(params int[] scriptIds) =>
        new ObjectProperty { Field = ObjectField.ScriptsIdx, RawBytes = [0] }.WithInt32Array(scriptIds);

    private static ObjectProperty MakeArtProperty(ObjectField field, uint artId) =>
        ObjectPropertyFactory.ForInt32(field, unchecked((int)artId));

    private static TileScript MakeTileScript(int scriptId) =>
        new()
        {
            NodeFlags = 0,
            TileId = 0,
            ScriptFlags = 0,
            ScriptCounters = 0,
            ScriptNum = scriptId,
        };

    private static SectorLight MakeLight(uint artId) =>
        new()
        {
            ObjHandle = -1,
            TileLoc = 0,
            OffsetX = 0,
            OffsetY = 0,
            Flags = SectorLightFlags.None,
            ArtId = artId,
            R = 0,
            B = 0,
            G = 0,
            TintColor = 0,
            Palette = 0,
            Padding2C = 0,
        };

    private static GameObjectGuid MakeProtoId(int protoNumber)
    {
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, protoNumber);
        return new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, new Guid(bytes));
    }
}
