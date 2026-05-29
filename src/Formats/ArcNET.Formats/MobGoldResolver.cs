using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Formats;

/// <summary>
/// Resolves carried-gold data across critter and gold-object MOB records.
/// </summary>
public static class MobGoldResolver
{
    private const int GoldProtoNumber = 9056;

    /// <summary>
    /// Returns the scalar gold quantity stored on a gold item, or <see langword="null"/> when
    /// the object is not a gold item or the quantity field is missing/invalid.
    /// </summary>
    public static int? GetGoldQuantity(MobData mob)
    {
        ArgumentNullException.ThrowIfNull(mob);

        if (mob.Header.GameObjectType is not ObjectType.Gold)
            return null;

        var quantityProperty = mob.GetProperty(ObjectField.GoldQuantity);
        if (quantityProperty is null)
            return null;

        try
        {
            return quantityProperty.GetInt32();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the gold-item handle stored on a critter, or <see langword="null"/> when the
    /// object is not a critter or the handle field is missing/invalid.
    /// </summary>
    public static GameObjectGuid? GetCritterGoldHandle(MobData mob)
    {
        ArgumentNullException.ThrowIfNull(mob);

        if (mob.Header.GameObjectType is not ObjectType.Pc and not ObjectType.Npc)
            return null;

        var goldHandleProperty = mob.GetProperty(ObjectField.CritterGold);
        if (goldHandleProperty is null)
            return null;

        try
        {
            return goldHandleProperty.GetObjectId();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the inventory object ids stored on a container, or an empty list when the object
    /// is not a container or the inventory field is missing/invalid.
    /// </summary>
    public static IReadOnlyList<Guid> GetContainerInventoryObjectIds(MobData mob)
    {
        ArgumentNullException.ThrowIfNull(mob);

        if (mob.Header.GameObjectType is not ObjectType.Container)
            return [];

        var inventoryProperty = mob.GetProperty(ObjectField.ContainerInventoryListIdx);
        if (inventoryProperty is null)
            return [];

        try
        {
            return inventoryProperty.GetObjectIdArray();
        }
        catch (InvalidOperationException)
        {
            return [];
        }
    }

    /// <summary>
    /// Resolves gold quantity through a critter's gold-item handle.
    /// </summary>
    public static int? ResolveCritterGoldQuantity(MobData critter, Func<GameObjectGuid, MobData?> mobResolver)
    {
        ArgumentNullException.ThrowIfNull(critter);
        ArgumentNullException.ThrowIfNull(mobResolver);

        return GetCritterGoldHandle(critter) is { } goldHandle ? ResolveGoldQuantity(goldHandle, mobResolver) : null;
    }

    /// <summary>
    /// Resolves gold quantity through one already-decoded gold-item handle.
    /// </summary>
    public static int? ResolveGoldQuantity(GameObjectGuid goldHandle, Func<GameObjectGuid, MobData?> mobResolver)
    {
        ArgumentNullException.ThrowIfNull(mobResolver);

        return mobResolver(goldHandle) is { } goldMob ? GetGoldQuantity(goldMob) : null;
    }

    /// <summary>
    /// Resolves container gold using the same rule as <c>arcanum-ce</c>: find the first
    /// inventory item whose prototype is <c>BP_GOLD</c> (proto 9056), then read that item's
    /// <see cref="ObjectField.GoldQuantity"/> scalar.
    /// </summary>
    public static int? ResolveContainerGoldQuantity(MobData container, Func<GameObjectGuid, MobData?> mobResolver)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(mobResolver);

        if (container.Header.GameObjectType is not ObjectType.Container)
            return null;

        var inventoryProperty = container.GetProperty(ObjectField.ContainerInventoryListIdx);
        if (inventoryProperty is null)
            return null;

        Guid[] itemIds;
        try
        {
            itemIds = inventoryProperty.GetObjectIdArray();
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        for (var itemIndex = 0; itemIndex < itemIds.Length; itemIndex++)
        {
            var itemObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 0, itemIds[itemIndex]);
            if (mobResolver(itemObjectId) is not { } itemMob)
                continue;

            var protoNumber = itemMob.Header.ProtoId.GetProtoNumber();
            if (
                protoNumber != GoldProtoNumber
                && !(protoNumber is null && itemMob.Header.GameObjectType == ObjectType.Gold)
            )
                continue;

            return GetGoldQuantity(itemMob);
        }

        return null;
    }
}
