using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Formats.Tests;

public sealed class MobGoldResolverTests
{
    private static MobData MakeMob(
        ObjectType type,
        GameObjectGuid objectId,
        GameObjectGuid protoId,
        params ObjectProperty[] properties
    ) =>
        new MobData
        {
            Header = new GameObjectHeader
            {
                Version = 0x77,
                ProtoId = protoId,
                ObjectId = objectId,
                GameObjectType = type,
                PropCollectionItems = 0,
                Bitmap = new byte[type is ObjectType.Pc or ObjectType.Npc ? 20 : 16],
            },
            Properties = properties,
        }.RebuildHeader();

    [Test]
    public async Task GetGoldQuantity_GoldMob_ReturnsQuantity()
    {
        var goldMob = MakeMob(
            ObjectType.Gold,
            new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 100, Guid.NewGuid()),
            new GameObjectGuid(GameObjectGuid.OidTypeP, 0, 9056, Guid.Empty),
            ObjectPropertyFactory.ForInt32(ObjectField.GoldQuantity, 4321)
        );

        await Assert.That(MobGoldResolver.GetGoldQuantity(goldMob)).IsEqualTo(4321);
    }

    [Test]
    public async Task ResolveCritterGoldQuantity_HandlePointsAtGoldMob_ReturnsQuantity()
    {
        var goldObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 200, Guid.NewGuid());
        var critter = MakeMob(
            ObjectType.Npc,
            new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 101, Guid.NewGuid()),
            new GameObjectGuid(GameObjectGuid.OidTypeP, 0, 1001, Guid.Empty),
            ObjectPropertyFactory.ForObjectId(ObjectField.CritterGold, goldObjectId)
        );
        var goldMob = MakeMob(
            ObjectType.Gold,
            goldObjectId,
            new GameObjectGuid(GameObjectGuid.OidTypeP, 0, 9056, Guid.Empty),
            ObjectPropertyFactory.ForInt32(ObjectField.GoldQuantity, 999)
        );

        var resolved = MobGoldResolver.ResolveCritterGoldQuantity(
            critter,
            objectId => objectId == goldObjectId ? goldMob : null
        );

        await Assert.That(resolved).IsEqualTo(999);
    }

    [Test]
    public async Task ResolveCritterGoldQuantity_MissingTarget_ReturnsNull()
    {
        var critter = MakeMob(
            ObjectType.Pc,
            new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 102, Guid.NewGuid()),
            new GameObjectGuid(GameObjectGuid.OidTypeP, 0, 1002, Guid.Empty),
            ObjectPropertyFactory.ForObjectId(
                ObjectField.CritterGold,
                new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 300, Guid.NewGuid())
            )
        );

        await Assert.That(MobGoldResolver.ResolveCritterGoldQuantity(critter, _ => null)).IsNull();
    }

    [Test]
    public async Task ResolveContainerGoldQuantity_UsesGoldPrototypeItem()
    {
        var goldObjectIdA = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 401, Guid.NewGuid());
        var goldObjectIdB = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 402, Guid.NewGuid());
        var container = MakeMob(
            ObjectType.Container,
            new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 400, Guid.NewGuid()),
            new GameObjectGuid(GameObjectGuid.OidTypeP, 0, 1003, Guid.Empty),
            ObjectPropertyFactory.ForObjectIdArray(
                ObjectField.ContainerInventoryListIdx,
                [goldObjectIdA.Id, goldObjectIdB.Id]
            )
        );
        var goldMobA = MakeMob(
            ObjectType.Gold,
            goldObjectIdA,
            new GameObjectGuid(GameObjectGuid.OidTypeP, 0, 9056, Guid.Empty),
            ObjectPropertyFactory.ForInt32(ObjectField.GoldQuantity, 111)
        );
        var goldMobB = MakeMob(
            ObjectType.Gold,
            goldObjectIdB,
            new GameObjectGuid(GameObjectGuid.OidTypeP, 0, 9056, Guid.Empty),
            ObjectPropertyFactory.ForInt32(ObjectField.GoldQuantity, 222)
        );

        var resolved = MobGoldResolver.ResolveContainerGoldQuantity(
            container,
            objectId =>
                objectId.Id == goldObjectIdA.Id ? goldMobA
                : objectId.Id == goldObjectIdB.Id ? goldMobB
                : null
        );

        await Assert.That(resolved).IsEqualTo(111);
    }

    [Test]
    public async Task ResolveContainerGoldQuantity_IgnoresNonGoldPrototypeItems()
    {
        var itemObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 501, Guid.NewGuid());
        var goldObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 502, Guid.NewGuid());
        var container = MakeMob(
            ObjectType.Container,
            new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 500, Guid.NewGuid()),
            new GameObjectGuid(GameObjectGuid.OidTypeP, 0, 1003, Guid.Empty),
            ObjectPropertyFactory.ForObjectIdArray(
                ObjectField.ContainerInventoryListIdx,
                [itemObjectId.Id, goldObjectId.Id]
            )
        );
        var normalItem = MakeMob(
            ObjectType.Generic,
            itemObjectId,
            new GameObjectGuid(GameObjectGuid.OidTypeP, 0, 7000, Guid.Empty),
            ObjectPropertyFactory.ForInt32(ObjectField.GoldQuantity, 9999)
        );
        var goldMob = MakeMob(
            ObjectType.Gold,
            goldObjectId,
            new GameObjectGuid(GameObjectGuid.OidTypeP, 0, 9056, Guid.Empty),
            ObjectPropertyFactory.ForInt32(ObjectField.GoldQuantity, 250)
        );

        var resolved = MobGoldResolver.ResolveContainerGoldQuantity(
            container,
            objectId =>
                objectId.Id == itemObjectId.Id ? normalItem
                : objectId.Id == goldObjectId.Id ? goldMob
                : null
        );

        await Assert.That(resolved).IsEqualTo(250);
    }
}
